using Microsoft.Extensions.Logging;
using BottleGame.Application.Common;
using BottleGame.Infrastructure.Data;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using BottleGame.Application.Interfaces;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис для работы с игровыми раундами
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="GameRoundService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    /// <param name="cacheService">Сервис кэширования</param>
    public class GameRoundService(ApplicationDbContext context, ILogger<GameRoundService> logger, ICacheService cacheService) : IGameRoundService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<GameRoundService> _logger = logger;

        /// <summary>
        /// Сервис кэширования
        /// </summary>
        private readonly ICacheService _cacheService = cacheService;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[GameRoundService]";

        #endregion Fields

        #region Public Methods

        /// <inheritdoc/>
        public async Task<Result> FinishGameRound(int playerId, FinishRoundRequest request, int? requestingPlayerId, bool isAdmin)
        {
            // Проверка прав доступа
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.GameRounds)
                .Include(p => p.Balance)
                .Include(p => p.Ban)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result.Forbidden("Пользователь в бане");
            }

            if (player.Balance.Amount < request.Bet && !request.IsWin)
            {
                _logger.LogWarning("{Prefix} Недостаточно средств на балансе для снятия у игрока {PlayerId}", Prefix, playerId);
                return Result.Failure("Недостаточно средств!");
            }

            var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == playerId);

            if (roundsPlayedStat is null)
            {
                _logger.LogWarning("{Prefix} Не найдены данные статистики сыгранных раундов для игрока {PlayerId}", Prefix, playerId);
                return Result.Failure("Не найдены данные статистики сыгранных раундов игрока");
            }

            roundsPlayedStat.PlayedRoundsCount++;

            // Создание записи о раунде
            var round = new GameRound
            {
                Player = player,
                PlayerId = playerId,
                Bet = request.Bet,
                IsWin = request.IsWin,
                FinishedAt = DateTime.UtcNow
            };

            _context.GameRounds.Add(round);

            // Обновление баланса игрока
            player.Balance.Amount += request.IsWin ? request.Bet : -request.Bet;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "{Prefix} Раунд завершен для игрока {PlayerId}. Статус: {Status}, Сумма: {Bet}",
                Prefix,
                playerId,
                request.IsWin ? "Победа" : "Проигрыш",
                request.Bet);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<GetPlayerPlayedRoundsResponse>> GetPlayerPlayedRounds(int playerId)
        {
            var player = await _context.Players
                .Include(p => p.GameRounds)
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<GetPlayerPlayedRoundsResponse>.NotFound($"Игрок с ID {playerId} не найден");
            }

            var response = new GetPlayerPlayedRoundsResponse
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
                Rounds = [.. player.GameRounds.Select(t => new RoundDTO
                {
                    Id = t.Id,
                    Bet = t.Bet,
                    IsWin = t.IsWin,
                    FinishedAt = t.FinishedAt
                })]
            };

            _logger.LogInformation("{Prefix} Получен список сыгранных раундов игрока {PlayerId}", Prefix, playerId);

            return Result<GetPlayerPlayedRoundsResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<GetProfitResponse>> GetProfit(GetProfitRequest request)
        {
            var query = _context.GameRounds.AsNoTracking().AsQueryable();

            if (request.DateFrom.HasValue)
            {
                query = query.Where(r => r.FinishedAt > request.DateFrom.Value);
            }

            if (request.DateTill.HasValue)
            {
                query = query.Where(r => r.FinishedAt < request.DateTill.Value);
            }

            // Проверяем наличие данных без загрузки всех записей
            var hasRounds = await query.AnyAsync();

            if (!hasRounds)
            {
                _logger.LogWarning("{Prefix} Раунды игроков не найдены", Prefix);
                return Result<GetProfitResponse>.NotFound("Раунды игроков не найдены, нет возможности посчитать прибыль");
            }

            // Используем агрегатные функции SQL вместо загрузки всех записей в память
            var profit = await query
                .Where(r => !r.IsWin)
                .SumAsync(r => r.Bet);

            var dateFrom = await query.MinAsync(r => r.FinishedAt);
            var dateTill = await query.MaxAsync(r => r.FinishedAt);

            _logger.LogInformation("{Prefix} Прибыль рассчитана за период {DateFrom} - {DateTill}",
                Prefix, dateFrom, dateTill);

            var response = new GetProfitResponse
            {
                Profit = profit,
                DateFrom = dateFrom,
                DateTill = dateTill
            };

            return Result<GetProfitResponse>.Success(response);
        }

        #endregion Public Methods
    }
}




