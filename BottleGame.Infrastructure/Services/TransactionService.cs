using Microsoft.Extensions.Logging;
using BottleGame.Application.Common;
using BottleGame.Domain.Constants;
using BottleGame.Infrastructure.Data;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.Services.Payments;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис для работы с транзакциями
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="TransactionService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    /// <param name="cacheService">Сервис кэширования</param>
    /// <param name="processor">Обработчик пополнений и выводов для текущего режима процессинга</param>
    public class TransactionService(
        ApplicationDbContext context,
        ILogger<TransactionService> logger,
        ICacheService cacheService,
        ITransactionProcessor processor) : ITransactionService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<TransactionService> _logger = logger;

        /// <summary>
        /// Сервис кэширования
        /// </summary>
        private readonly ICacheService _cacheService = cacheService;

        /// <summary>
        /// Обработчик пополнений и выводов для текущего режима процессинга
        /// </summary>
        private readonly ITransactionProcessor _processor = processor;

        /// <summary>
        /// Префикс для логов
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
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<GetPlayerBalanceResponse>.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result<GetPlayerBalanceResponse>.Forbidden("Пользователь в бане");
            }

            var balance = await _context.PlayersBalances
                .AsNoTracking()
                .Where(b => b.PlayerId == playerId)
                .Select(b => b.Amount)
                .FirstOrDefaultAsync();

            if (balance == default)
            {
                _logger.LogError("{Prefix} Баланс игрока не найден! Нарушена целостность данных", Prefix);
            }

            _logger.LogInformation("{Prefix} Успешно получен баланс для игрока {PlayerId}: {Amount}", Prefix, playerId, balance);

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
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<object?>.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result<object?>.Forbidden("Пользователь в бане");
            }

            if (!request.IsDeposit && player.Balance.Amount < request.Amount)
            {
                _logger.LogWarning("{Prefix} Недостаточно средств на балансе для снятия у игрока {PlayerId}", Prefix, playerId);
                return Result<object?>.Failure("Недостаточно средств!");
            }

            var gameParams = await _cacheService.GetGameParamsAsync();
            if (gameParams is null)
            {
                _logger.LogWarning("{Prefix} Не найдены игровые параметры", Prefix);
                return Result<object?>.Failure("Не найдены игровые параметры");
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

            await _context.SaveChangesAsync();

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
                "{Prefix} Транзакция создана для игрока {PlayerId}. Тип: {Status}, Сумма: {Bet}",
                Prefix,
                playerId,
                request.IsDeposit ? "Пополнение" : "Вывод",
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
                _logger.LogWarning("{Prefix} Некорректный статус транзакции: {StatusId}", Prefix, request.TransactionStatusId);
                return Result.Failure($"Допустимые статусы: {approvedStatusId}-Одобрено, {rejectedStatusId}-Отклонено");
            }

            var transaction = await _context.MoneyTransactions
                .Include(t => t.Player).ThenInclude(p => p.PlayerOptions)
                .Include(t => t.Player).ThenInclude(p => p.Balance)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction is null)
            {
                _logger.LogWarning("{Prefix} Транзакция с ID {TransactionId} не найдена", Prefix, transactionId);
                return Result.NotFound($"Транзакция с ID {transactionId} не найдена");
            }

            if (transaction.Player is null || transaction.Player.Balance is null || transaction.Player.PlayerOptions is null)
            {
                _logger.LogWarning("{Prefix} Данные транзакции {TransactionId} неполные", Prefix, transaction.Id);
                return Result.NotFound($"Данные транзакции {transaction.Id} неполные");
            }

            if (transaction.TransactionTypeId != withdrawalTypeId)
            {
                _logger.LogWarning("{Prefix} Тип транзакции {TransactionId} не поддерживается для ручного одобрения/отклонения", Prefix, transactionId);
                return Result.Failure("Возможно изменять статус только у транзакций с типом \"Вывод\"");
            }

            if (transaction.TransactionStatusId != inWaitStatusId)
            {
                _logger.LogWarning("{Prefix} Транзакция {TransactionId} уже обработана (статус {StatusId})", Prefix, transactionId, transaction.TransactionStatusId);
                return Result.Failure("Нельзя изменить статус уже обработанной транзакции");
            }

            if (transaction.TransactionStatusId == request.TransactionStatusId)
            {
                _logger.LogWarning("{Prefix} Транзакция {TransactionId} уже имеет статус {StatusId}", Prefix, transactionId, request.TransactionStatusId);
                return Result.Failure("Транзакция уже имеет указанный статус");
            }

            var gameParams = await _cacheService.GetGameParamsAsync();
            if (gameParams is null)
            {
                return Result.Failure("Не найдены игровые параметры");
            }

            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                return Result.Failure("Не найдены данные баланса приложения");
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
                _logger.LogWarning("{Prefix} Транзакция {TransactionId} уже изменена другим запросом", Prefix, transactionId);
                return Result.Conflict("Транзакция уже изменена другим запросом");
            }

            _logger.LogInformation("{Prefix} Статус транзакции {TransactionId} изменен на {StatusId}", Prefix, transactionId, request.TransactionStatusId);

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
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<GetPlayerTransactionsResponse>.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<GetPlayerTransactionsResponse>.Unauthorized();
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result<GetPlayerTransactionsResponse>.Forbidden("Пользователь в бане");
            }

            var transactions = await _context.MoneyTransactions
                .Include(t => t.TransactionType)
                .Include(t => t.TransactionStatus)
                .Where(t => t.PlayerId == playerId)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("{Prefix} Получено {Count} транзакций для игрока {PlayerId}", Prefix, transactions.Count, playerId);

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
        public async Task<Result<IEnumerable<GetPlayerTransactionsResponse>>> GetAllTransactions(int? typeId)
        {
            var bannedPlayerIds = await _context.GetActivelyBannedPlayerIdsAsync();

            var query = _context.MoneyTransactions
                .AsNoTracking()
                .WherePlayerNotActivelyBanned(bannedPlayerIds)
                .AsQueryable();

            if (typeId is not null)
            {
                query = query.Where(t => t.TransactionTypeId == typeId);
            }

            var rows = await query
                .OrderByDescending(t => t.CreatedAt)
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

            _logger.LogInformation("{Prefix} Получено {Count} транзакций", Prefix, rows.Count);

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

            _logger.LogInformation("{Prefix} Выведены статусы транзакций (из кэша)", Prefix);

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

            _logger.LogInformation("{Prefix} Выведены типы транзакций (из кэша)", Prefix);

            return Result<IEnumerable<TransactionStatusOrTypeDTO>>.Success(types);
        }

        #endregion Reference Data Methods
    }
}