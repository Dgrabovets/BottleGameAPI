using Microsoft.Extensions.Logging;
using BottleGame.Application.Common;
using BottleGame.Domain.Constants;
using BottleGame.Infrastructure.Data;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using BottleGame.Application.Interfaces;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис для работы с банами игроков
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="BanService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    public class BanService(ApplicationDbContext context, ILogger<BanService> logger) : IBanService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<BanService> _logger = logger;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[BanService]";

        /// <summary>
        /// Максимальное количество попыток при конфликте параллелизма
        /// </summary>
        private const int MaxRetries = 3;

        #endregion Fields

        #region Public Methods

        /// <inheritdoc/>
        public async Task<Result<IsBannedResponse>> IsBanned(int playerId, int? requestingPlayerId, bool isAdmin)
        {
            // Проверка прав доступа
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<IsBannedResponse>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<IsBannedResponse>.NotFound($"Игрок с ID {playerId} не найден");
            }

            var banHistory = await _context.BansHistory
                .Where(b => b.PlayerId == playerId)
                .OrderByDescending(b => b.BannedAt)
                .Select(b => new BanHistoryItem
                {
                    BanReason = b.BanReason,
                    BannedAt = b.BannedAt,
                    UnbannedAt = b.UnbannedAt
                })
                .AsNoTracking()
                .ToListAsync();

            var response = new IsBannedResponse
            {
                IsBanned = player.Ban is not null && player.Ban.IsActive,
                BanReason = player.Ban?.BanReason ?? string.Empty,
                BanHistory = banHistory
            };

            _logger.LogInformation("{Prefix} Проверка бана для игрока {PlayerId}. Результат: {IsBanned}",
                Prefix, playerId, response.IsBanned);

            return Result<IsBannedResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result> BanPlayer(BanPlayerRequest request)
        {
            int attempt = 0;

            while (attempt < MaxRetries)
            {
                try
                {
                    var player = await _context.Players
                        .Include(p => p.Ban)
                        .AsTracking()
                        .FirstOrDefaultAsync(p => p.Id == request.PlayerId);

                    if (player is null)
                    {
                        _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, request.PlayerId);
                        return Result.NotFound($"Игрок с ID {request.PlayerId} не найден");
                    }

                    if (player.Ban is not null && player.Ban.IsActive)
                    {
                        _logger.LogWarning("{Prefix} Игрок {PlayerId} уже забанен", Prefix, request.PlayerId);
                        return Result.Conflict("Пользователь уже имеет активный бан");
                    }

                    // Создание или обновление бана
                    if (player.Ban is not null)
                    {
                        player.Ban.IsActive = true;
                        player.Ban.BanReason = request.BanReason;
                        player.Ban.BannedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var ban = new Ban
                        {
                            Player = player,
                            PlayerId = request.PlayerId,
                            BanReason = request.BanReason,
                            BannedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.Bans.Add(ban);
                    }

                    // Добавление в историю
                    var banHistory = new BanHistoryEntity
                    {
                        Player = player,
                        PlayerId = request.PlayerId,
                        BanReason = request.BanReason,
                        BannedAt = DateTime.UtcNow
                    };
                    _context.BansHistory.Add(banHistory);

                    // Отклоняем все ожидающие выводы
                    var transactions = await _context.MoneyTransactions
                        .Where(mt => mt.PlayerId == request.PlayerId &&
                                    mt.TransactionTypeId == TransactionTypes.Withdrawal &&
                                    mt.TransactionStatusId == TransactionStatuses.Pending)
                        .ToListAsync();

                    foreach (var t in transactions)
                    {
                        t.TransactionStatusId = TransactionStatuses.Rejected;
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("{Prefix} Игрок {PlayerId} забанен", Prefix, request.PlayerId);
                    return Result.Success();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    attempt++;
                    _context.ChangeTracker.Clear();

                    if (attempt == MaxRetries)
                    {
                        _logger.LogError(ex, "{Prefix} Ошибка параллелизма при бане игрока {PlayerId} после {Attempt} попыток",
                            Prefix, request.PlayerId, attempt);
                        return Result.InternalError("Не удалось выполнить операцию из-за конфликта данных");
                    }

                    await Task.Delay(100 * attempt);
                }
            }

            return Result.InternalError();
        }

        /// <inheritdoc/>
        public async Task<Result> UnbanPlayer(int playerId)
        {
            int attempt = 0;

            while (attempt < MaxRetries)
            {
                try
                {
                    var player = await _context.Players
                        .Include(p => p.Ban)
                        .AsTracking()
                        .FirstOrDefaultAsync(p => p.Id == playerId);

                    if (player is null)
                    {
                        _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                        return Result.NotFound($"Игрок с ID {playerId} не найден");
                    }

                    if (player.Ban is null || !player.Ban.IsActive)
                    {
                        _logger.LogWarning("{Prefix} Игрок {PlayerId} не имеет активного бана", Prefix, playerId);
                        return Result.NotFound("Пользователь не имеет активного бана");
                    }

                    // Обновление бана
                    player.Ban.IsActive = false;

                    // Обновление истории
                    var lastBan = await _context.BansHistory
                        .Where(b => b.PlayerId == playerId && b.UnbannedAt == null)
                        .OrderByDescending(b => b.BannedAt)
                        .FirstOrDefaultAsync();

                    if (lastBan is not null)
                    {
                        lastBan.UnbannedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("{Prefix} Игрок {PlayerId} разбанен", Prefix, playerId);
                    return Result.Success();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    attempt++;
                    _context.ChangeTracker.Clear();

                    if (attempt == MaxRetries)
                    {
                        _logger.LogError(ex, "{Prefix} Ошибка параллелизма при разбане игрока {PlayerId} после {Attempt} попыток",
                            Prefix, playerId, attempt);
                        return Result.InternalError("Не удалось выполнить операцию из-за конфликта данных");
                    }

                    await Task.Delay(100 * attempt);
                }
            }

            return Result.InternalError();
        }

        #endregion Public Methods
    }
}




