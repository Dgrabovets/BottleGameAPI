using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static BottleGame.Infrastructure.Services.PlayerMapper;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Player statistics service
    /// </summary>
    public class PlayerStatisticsService : IPlayerStatisticsService
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<PlayerStatisticsService> _logger;

        /// <summary>
        /// Cache service
        /// </summary>
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[PlayerStatisticsService]";

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="PlayerStatisticsService"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger</param>
        /// <param name="cacheService">Cache service</param>
        public PlayerStatisticsService(ApplicationDbContext context, ILogger<PlayerStatisticsService> logger, ICacheService cacheService)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
        }

        #region Statistics Methods

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<PlayerStatisticsResponse>>> GetAllPlayersStatistics()
        {
            var players = await _context.Players
                .Include(p => p.Balance)
                .Include(p => p.GameRounds)
                .Include(p => p.Ban)
                .AsNoTracking()
                .ToListAsync();

            var response = players.Select(p => new PlayerStatisticsResponse
            {
                Player = MapToPlayerDTO(p),
                BannedAt = p.Ban is not null && p.Ban.IsActive ? p.Ban.BannedAt : null,
                Balance = p.Balance.Amount,
                RoundsQty = p.GameRounds.Count,
                WinsQty = p.GameRounds.Count(r => r.IsWin),
                LossQty = p.GameRounds.Count(r => !r.IsWin),
                WinAmount = p.GameRounds.Where(r => r.IsWin).Sum(r => r.Bet),
                LossAmount = p.GameRounds.Where(r => !r.IsWin).Sum(r => r.Bet)
            }).ToList();

            _logger.LogInformation("{Prefix} Got the statistics of {Count} players", Prefix, response.Count);

            return Result<IEnumerable<PlayerStatisticsResponse>>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<PlayerStatisticsResponse>> GetPlayerStatistics(int playerId, int? requestingPlayerId, bool isAdmin)
        {
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<PlayerStatisticsResponse>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.Balance)
                .Include(p => p.GameRounds)
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result<PlayerStatisticsResponse>.NotFound($"Player with id {playerId} not found");
            }

            var response = new PlayerStatisticsResponse
            {
                Player = MapToPlayerDTO(player),
                BannedAt = player.Ban is not null && player.Ban.IsActive ? player.Ban.BannedAt : null,
                Balance = player.Balance.Amount,
                RoundsQty = player.GameRounds.Count,
                WinsQty = player.GameRounds.Count(r => r.IsWin),
                LossQty = player.GameRounds.Count(r => !r.IsWin),
                WinAmount = player.GameRounds.Where(r => r.IsWin).Sum(r => r.Bet),
                LossAmount = player.GameRounds.Where(r => !r.IsWin).Sum(r => r.Bet)
            };

            _logger.LogInformation("{Prefix} Got the statistics of player - {PlayerId}", Prefix, playerId);

            return Result<PlayerStatisticsResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<PlayerRoundsStatisticsForWithdrawalDTO>> GetPlayerRoundsStatisticsForWithdrawal(
            int playerId, int? requestingPlayerId, bool isAdmin)
        {
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.NotFound($"Player with id {playerId} not found");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} User is banned", Prefix);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.Forbidden("User is banned");
            }

            var stat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .Include(s => s.MoneyTransaction)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.PlayerId == playerId);

            if (stat is null)
            {
                _logger.LogWarning("{Prefix} Statistics of the played rounds for player {PlayerId} not found", Prefix, playerId);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.NotFound($"Statistics of the played rounds for player {playerId} not found");
            }

            var transactionTypes = await _cacheService.GetTransactionTypesAsync();
            var transactionStatuses = await _cacheService.GetTransactionStatusesAsync();

            if (transactionStatuses.Count == 0 || transactionTypes.Count == 0)
            {
                _logger.LogWarning("{Prefix} Transaction types or statuses not found", Prefix);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.InternalError("Transaction types or statuses not found");
            }

            TransactionDTO? lastDeposit = null;
            if (stat.MoneyTransaction is not null)
            {
                lastDeposit = new TransactionDTO
                {
                    Id = stat.MoneyTransaction.Id,
                    Amount = stat.MoneyTransaction.Amount,
                    StatusId = stat.MoneyTransaction.TransactionStatusId,
                    StatusName = transactionStatuses.GetValueOrDefault(stat.MoneyTransaction.TransactionStatusId),
                    TypeId = stat.MoneyTransaction.TransactionTypeId,
                    TypeName = transactionTypes.GetValueOrDefault(stat.MoneyTransaction.TransactionTypeId),
                    CreatedAt = stat.MoneyTransaction.CreatedAt,
                    ProcessedAt = stat.MoneyTransaction.ProcessedAt
                };
            }

            var dto = new PlayerRoundsStatisticsForWithdrawalDTO
            {
                Player = MapToPlayerDTO(player),
                LastDeposit = lastDeposit,
                TotalRoundsPlayed = stat.PlayedRoundsCount
            };

            return Result<PlayerRoundsStatisticsForWithdrawalDTO>.Success(dto);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>> GetAllPlayersRoundsStatisticsForWithdrawal()
        {
            var stats = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .Include(s => s.MoneyTransaction)
                .Include(s => s.Player)
                .AsNoTracking()
                .ToListAsync();

            if (stats.Count == 0)
            {
                _logger.LogWarning("{Prefix} Statistics of the played rounds not found", Prefix);
                return Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>.NotFound("Statistics of the played rounds not found");
            }

            var transactionTypes = await _cacheService.GetTransactionTypesAsync();
            var transactionStatuses = await _cacheService.GetTransactionStatusesAsync();

            if (transactionStatuses.Count == 0 || transactionTypes.Count == 0)
            {
                _logger.LogWarning("{Prefix} Transaction types or statuses not found", Prefix);
                return Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>.InternalError("Transaction types or statuses not found");
            }

            var dtos = stats.Select(stat =>
            {
                var player = stat.Player;

                TransactionDTO? lastDeposit = null;
                if (stat.MoneyTransaction is not null)
                {
                    lastDeposit = new TransactionDTO
                    {
                        Id = stat.MoneyTransaction.Id,
                        Amount = stat.MoneyTransaction.Amount,
                        StatusId = stat.MoneyTransaction.TransactionStatusId,
                        StatusName = transactionStatuses.GetValueOrDefault(stat.MoneyTransaction.TransactionStatusId),
                        TypeId = stat.MoneyTransaction.TransactionTypeId,
                        TypeName = transactionTypes.GetValueOrDefault(stat.MoneyTransaction.TransactionTypeId),
                        CreatedAt = stat.MoneyTransaction.CreatedAt,
                        ProcessedAt = stat.MoneyTransaction.ProcessedAt
                    };
                }

                return new PlayerRoundsStatisticsForWithdrawalDTO
                {
                    Player = MapToPlayerDTO(player),
                    LastDeposit = lastDeposit,
                    TotalRoundsPlayed = stat.PlayedRoundsCount
                };
            });

            return Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>.Success(dtos);
        }

        #endregion Statistics Methods
    }
}
