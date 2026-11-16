using Microsoft.Extensions.Logging;
using BottleGame.Application.Common;
using BottleGame.Infrastructure.Data;
using BottleGame.Application.DTOs.Responses;
using Microsoft.EntityFrameworkCore;
using BottleGame.Application.Interfaces;
using static BottleGame.Infrastructure.Services.PlayerMapper;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис статистики игроков
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="PlayerStatisticsService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    /// <param name="cacheService">Сервис кэширования</param>
    public class PlayerStatisticsService(
        ApplicationDbContext context,
        ILogger<PlayerStatisticsService> logger,
        ICacheService cacheService) : IPlayerStatisticsService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<PlayerStatisticsService> _logger = logger;

        /// <summary>
        /// Сервис кэширования
        /// </summary>
        private readonly ICacheService _cacheService = cacheService;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[PlayerStatisticsService]";

        #endregion Fields

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

            _logger.LogInformation("{Prefix} Получена статистика по {Count} игрокам", Prefix, response.Count);

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
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<PlayerStatisticsResponse>.NotFound($"Игрок с ID {playerId} не найден");
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

            _logger.LogInformation("{Prefix} Получена статистика по игроку - {PlayerId}", Prefix, playerId);

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
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.Forbidden("Пользователь в бане");
            }

            var stat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .Include(s => s.MoneyTransaction)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.PlayerId == playerId);

            if (stat is null)
            {
                _logger.LogWarning("{Prefix} Статистика сыгранных раундов для игрока {PlayerId} не найдена", Prefix, playerId);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.NotFound($"Статистика сыгранных раундов для игрока {playerId} не найдена");
            }

            var transactionTypes = await _cacheService.GetTransactionTypesAsync();
            var transactionStatuses = await _cacheService.GetTransactionStatusesAsync();

            if (transactionStatuses.Count == 0 || transactionTypes.Count == 0)
            {
                _logger.LogWarning("{Prefix} Типы или статусы транзакций не найдены", Prefix);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.InternalError("Типы или статусы транзакций не найдены");
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
                _logger.LogWarning("{Prefix} Статистика сыгранных раундов не найдена", Prefix);
                return Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>.NotFound("Статистика сыгранных раундов не найдена");
            }

            var transactionTypes = await _cacheService.GetTransactionTypesAsync();
            var transactionStatuses = await _cacheService.GetTransactionStatusesAsync();

            if (transactionStatuses.Count == 0 || transactionTypes.Count == 0)
            {
                _logger.LogWarning("{Prefix} Типы или статусы транзакций не найдены", Prefix);
                return Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>.InternalError("Типы или статусы транзакций не найдены");
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
