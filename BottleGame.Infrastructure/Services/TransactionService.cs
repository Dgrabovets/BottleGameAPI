using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Service for working with transactions
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="TransactionService"/>
    /// </remarks>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger</param>
    /// <param name="cacheService">Cache service</param>
    /// <param name="processor">Handler of deposits and withdrawals for the current processing mode</param>
    public class TransactionService(
        ApplicationDbContext context,
        ILogger<TransactionService> logger,
        ICacheService cacheService,
        ITransactionProcessor processor) : ITransactionService
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<TransactionService> _logger = logger;

        /// <summary>
        /// Cache service
        /// </summary>
        private readonly ICacheService _cacheService = cacheService;

        /// <summary>
        /// Handler of deposits and withdrawals for the current processing mode
        /// </summary>
        private readonly ITransactionProcessor _processor = processor;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[TransactionService]";

        #endregion Fields

        #region Balance Methods

        /// <inheritdoc/>
        public async Task<Result<GetPlayerBalanceResponse>> GetPlayerBalance(int playerId, int? requestingPlayerId, bool isAdmin)
        {
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<GetPlayerBalanceResponse>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result<GetPlayerBalanceResponse>.NotFound($"Player with id {playerId} not found");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} User is banned", Prefix);
                return Result<GetPlayerBalanceResponse>.Forbidden("User is banned");
            }

            var balance = await _context.PlayersBalances
                .AsNoTracking()
                .Where(b => b.PlayerId == playerId)
                .Select(b => b.Amount)
                .FirstOrDefaultAsync();

            if (balance == default)
            {
                _logger.LogError("{Prefix} Player balance not found! Data integrity is broken", Prefix);
            }

            _logger.LogInformation("{Prefix} Balance of player {PlayerId} retrieved successfully: {Amount}", Prefix, playerId, balance);

            return Result<GetPlayerBalanceResponse>.Success(new GetPlayerBalanceResponse { PlayerId = playerId, Balance = balance });
        }

        #endregion Balance Methods

        #region Transaction Methods

        /// <inheritdoc/>
        public async Task<Result<object?>> CreateTransaction(int playerId, MoneyTransactionRequest request, int? requestingPlayerId, bool isAdmin)
        {
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<object?>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.PlayerOptions)
                .Include(p => p.Balance)
                .Include(p => p.Ban)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result<object?>.NotFound($"Player with id {playerId} not found");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} User is banned", Prefix);
                return Result<object?>.Forbidden("User is banned");
            }

            if (!request.IsDeposit && player.Balance.Amount < request.Amount)
            {
                _logger.LogWarning("{Prefix} Insufficient balance for withdrawal for player {PlayerId}", Prefix, playerId);
                return Result<object?>.Failure("Insufficient funds!");
            }

            var gameParams = await _cacheService.GetGameParamsAsync();
            if (gameParams is null)
            {
                _logger.LogWarning("{Prefix} Game parameters not found", Prefix);
                return Result<object?>.Failure("Game parameters not found");
            }

            var requestCheck = await _processor.CheckRequestAsync(player, request.IsDeposit, gameParams);
            if (requestCheck.IsFailure)
            {
                return Result<object?>.Failure(requestCheck.Error!, requestCheck.StatusCode);
            }

            var transactionType = await _context.TransactionTypes.FirstOrDefaultAsync(t => t.Id == (request.IsDeposit ? TransactionTypes.Deposit : TransactionTypes.Withdrawal));
            var transactionStatus = await _context.TransactionStatuses.FirstOrDefaultAsync(s => s.Id == TransactionStatuses.Pending);

            var transaction = new MoneyTransaction
            {
                Player = player,
                PlayerId = playerId,
                Amount = request.Amount,
                TransactionTypeId = transactionType!.Id,
                TransactionType = transactionType,
                TransactionStatusId = transactionStatus!.Id,
                TransactionStatus = transactionStatus,
                CreatedAt = DateTime.UtcNow
            };

            _context.MoneyTransactions.Add(transaction);

            if (!request.IsDeposit)
            {
                player.Balance.Amount -= request.Amount;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request changed the balance after the funds check above, so the check may be stale.
                _logger.LogWarning("{Prefix} Concurrent balance update for player {PlayerId}", Prefix, playerId);
                return Result<object?>.Conflict("The balance was changed by another request, try again");
            }

            object? responseData = null;

            if (request.IsDeposit)
            {
                var depositResult = await _processor.StartDepositAsync(transaction, player);
                if (depositResult.IsFailure)
                {
                    return depositResult;
                }

                responseData = depositResult.Value;
            }

            _logger.LogInformation(
                "{Prefix} Transaction created for player {PlayerId}. Type: {Status}, Amount: {Bet}",
                Prefix,
                playerId,
                request.IsDeposit ? "Deposit" : "Withdrawal",
                request.Amount);

            return Result<object?>.Success(responseData);
        }

        /// <inheritdoc/>
        public async Task<Result> UpdateTransactionStatus(int transactionId, ChangeTransactionStatusRequest request)
        {
            var inWaitStatusId = TransactionStatuses.Pending;
            var rejectedStatusId = TransactionStatuses.Rejected;
            var approvedStatusId = TransactionStatuses.Approved;
            var withdrawalTypeId = TransactionTypes.Withdrawal;

            if (request.TransactionStatusId == inWaitStatusId)
            {
                _logger.LogWarning("{Prefix} Invalid transaction status: {StatusId}", Prefix, request.TransactionStatusId);
                return Result.Failure($"Allowed statuses: {approvedStatusId}-Approved, {rejectedStatusId}-Rejected");
            }

            var transaction = await _context.MoneyTransactions
                .Include(t => t.Player).ThenInclude(p => p.PlayerOptions)
                .Include(t => t.Player).ThenInclude(p => p.Balance)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction is null)
            {
                _logger.LogWarning("{Prefix} Transaction with ID {TransactionId} not found", Prefix, transactionId);
                return Result.NotFound($"Transaction with ID {transactionId} not found");
            }

            if (transaction.Player is null || transaction.Player.Balance is null || transaction.Player.PlayerOptions is null)
            {
                _logger.LogWarning("{Prefix} Data of transaction {TransactionId} is incomplete", Prefix, transaction.Id);
                return Result.NotFound($"Data of transaction {transaction.Id} is incomplete");
            }

            if (transaction.TransactionTypeId != withdrawalTypeId)
            {
                _logger.LogWarning("{Prefix} Type of transaction {TransactionId} does not support manual approval/rejection", Prefix, transactionId);
                return Result.Failure("The status can only be changed for transactions of type \"Withdrawal\"");
            }

            if (transaction.TransactionStatusId != inWaitStatusId)
            {
                _logger.LogWarning("{Prefix} Transaction {TransactionId} is already handled (status {StatusId})", Prefix, transactionId, transaction.TransactionStatusId);
                return Result.Failure("The status of an already handled transaction cannot be changed");
            }

            if (transaction.TransactionStatusId == request.TransactionStatusId)
            {
                _logger.LogWarning("{Prefix} Transaction {TransactionId} already has status {StatusId}", Prefix, transactionId, request.TransactionStatusId);
                return Result.Failure("The transaction already has the given status");
            }

            var gameParams = await _cacheService.GetGameParamsAsync();
            if (gameParams is null)
            {
                return Result.Failure("Game parameters not found");
            }

            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                return Result.Failure("Application balance data not found");
            }

            if (request.TransactionStatusId == rejectedStatusId)
            {
                transaction.Player.Balance.Amount += transaction.Amount;
                transaction.TransactionStatusId = request.TransactionStatusId;
            }
            else if (request.TransactionStatusId == approvedStatusId)
            {
                var approvalResult = await _processor.ApproveWithdrawalAsync(transaction, gameParams, appBalance);
                if (approvalResult.IsFailure)
                {
                    return approvalResult;
                }
            }

            transaction.ProcessedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("{Prefix} Transaction {TransactionId} was already changed by another request", Prefix, transactionId);
                return Result.Conflict("The transaction was already changed by another request");
            }

            _logger.LogInformation("{Prefix} Status of transaction {TransactionId} changed to {StatusId}", Prefix, transactionId, request.TransactionStatusId);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<GetPlayerTransactionsResponse>> GetPlayerTransactions(int playerId, int? requestingPlayerId, bool isAdmin)
        {
            var player = await _context.Players
                .Include(p => p.Ban)
                .Include(t => t.GameRounds)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result<GetPlayerTransactionsResponse>.NotFound($"Player with id {playerId} not found");
            }

            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<GetPlayerTransactionsResponse>.Unauthorized();
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} User is banned", Prefix);
                return Result<GetPlayerTransactionsResponse>.Forbidden("User is banned");
            }

            var transactions = await _context.MoneyTransactions
                .Include(t => t.TransactionType)
                .Include(t => t.TransactionStatus)
                .Where(t => t.PlayerId == playerId)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("{Prefix} Got {Count} transactions for player {PlayerId}", Prefix, transactions.Count, playerId);

            var response = new GetPlayerTransactionsResponse
            {
                Player = new PlayerDTO
                {
                    Id = playerId,
                    TelegramId = player.TelegramId,
                    Name = player.Name,
                    AvatarUrl = player.AvatarUrl,
                    RegisteredInAppAt = player.RegisteredInAppAt,
                    WinRate = player.WinRate,
                },
                Transactions = [.. transactions.Select(t => new TransactionDTO
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    StatusId = t.TransactionStatusId,
                    StatusName = t.TransactionStatus.Name,
                    TypeId = t.TransactionTypeId,
                    TypeName = t.TransactionType.Name,
                    CreatedAt = t.CreatedAt,
                    ProcessedAt = t.ProcessedAt
                })]
            };

            return Result<GetPlayerTransactionsResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<GetPlayerTransactionsResponse>>> GetAllTransactions(int? typeId, PageRequest? page = null)
        {
            var ordered = (await FilterTransactionsAsync(typeId))
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id);

            var paged = page is null ? ordered : ordered.Skip(page.Skip).Take(page.PageSize);

            var rows = await paged
                .Select(t => new
                {
                    t.Id,
                    t.Amount,
                    t.TransactionStatusId,
                    StatusName = t.TransactionStatus.Name,
                    t.TransactionTypeId,
                    TypeName = t.TransactionType.Name,
                    t.CreatedAt,
                    t.ProcessedAt,
                    t.PlayerId,
                    PlayerTelegramId = t.Player.TelegramId,
                    PlayerName = t.Player.Name,
                    PlayerAvatarUrl = t.Player.AvatarUrl,
                    PlayerRegisteredInAppAt = t.Player.RegisteredInAppAt,
                })
                .ToListAsync();

            _logger.LogInformation("{Prefix} Got {Count} transactions", Prefix, rows.Count);

            var response = rows
                .GroupBy(t => t.PlayerId)
                .Select(group =>
                {
                    var player = group.First();
                    return new GetPlayerTransactionsResponse
                    {
                        Player = new PlayerDTO
                        {
                            Id = player.PlayerId,
                            TelegramId = player.PlayerTelegramId,
                            Name = player.PlayerName,
                            AvatarUrl = player.PlayerAvatarUrl,
                            RegisteredInAppAt = player.PlayerRegisteredInAppAt,
                            WinRate = 0,
                        },
                        Transactions = [.. group.Select(t => new TransactionDTO
                        {
                            Id = t.Id,
                            Amount = t.Amount,
                            StatusId = t.TransactionStatusId,
                            StatusName = t.StatusName,
                            TypeId = t.TransactionTypeId,
                            TypeName = t.TypeName,
                            CreatedAt = t.CreatedAt,
                            ProcessedAt = t.ProcessedAt
                        })]
                    };
                })
                .ToList();

            return Result<IEnumerable<GetPlayerTransactionsResponse>>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<int> CountTransactions(int? typeId) =>
            await (await FilterTransactionsAsync(typeId)).CountAsync();

        /// <summary>
        /// Builds the transaction query of the list: transactions of actively banned players are left out
        /// </summary>
        /// <param name="typeId">Filter by type (optional)</param>
        /// <returns>Filtered query</returns>
        private async Task<IQueryable<MoneyTransaction>> FilterTransactionsAsync(int? typeId)
        {
            var bannedPlayerIds = await _context.GetActivelyBannedPlayerIdsAsync();

            var query = _context.MoneyTransactions
                .AsNoTracking()
                .WherePlayerNotActivelyBanned(bannedPlayerIds)
                .AsQueryable();

            return typeId is null ? query : query.Where(t => t.TransactionTypeId == typeId);
        }

        #endregion Transaction Methods

        #region Reference Data Methods

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionStatuses()
        {
            var statusesDict = await _cacheService.GetTransactionStatusesAsync();

            var statuses = statusesDict.Select(s => new TransactionStatusOrTypeDTO
            {
                Id = s.Key,
                Name = s.Value
            }).ToList();

            _logger.LogInformation("{Prefix} Transaction statuses returned (from the cache)", Prefix);

            return Result<IEnumerable<TransactionStatusOrTypeDTO>>.Success(statuses);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionTypes()
        {
            var typesDict = await _cacheService.GetTransactionTypesAsync();

            var types = typesDict.Select(t => new TransactionStatusOrTypeDTO
            {
                Id = t.Key,
                Name = t.Value
            }).ToList();

            _logger.LogInformation("{Prefix} Transaction types returned (from the cache)", Prefix);

            return Result<IEnumerable<TransactionStatusOrTypeDTO>>.Success(types);
        }

        #endregion Reference Data Methods
    }
}
