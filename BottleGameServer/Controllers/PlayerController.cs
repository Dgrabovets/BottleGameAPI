using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;
using BottleGameServer.Data;
using BottleGameServer.Models.DTOs.Requests;
using BottleGameServer.Models.DTOs.Responses;
using BottleGameServer.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BottleGameServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        #region Fields

        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlayerController> _logger;

        const string prefix = "[Player Controller]";

        #endregion Fields

        #region Constructor

        public PlayerController(ApplicationDbContext context, ILogger<PlayerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #endregion Constructor

        #region ActionMethods

        /// <summary>
        /// Проверить, забанен ли пользователь
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Информация о бане</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin,Moderator,Player")]
        [HttpGet("is-banned/{playerId:int}")]
        [ProducesResponseType(typeof(IsBannedResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IsBannedResponse>> IsBanned([FromRoute] int playerId)
        {
            try
            {
                if (User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                // Проверка существования игрока
                var player = await _context.Players
                    .Include(p => p.Ban)
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                // Получаем историю банов
                var banHistory = await _context.BansHistory
                    .Where(b => b.PlayerId == playerId)
                    .OrderByDescending(b => b.BannedAt)
                    .Select(b => new BanHistoryItem
                    {
                        BanReason = b.BanReason,
                        BannedAt = b.BannedAt,
                        UnbannedAt = b.UnbannedAt
                    })
                    .ToListAsync();

                // Формируем ответ
                var response = new IsBannedResponse
                {
                    IsBanned = player.Ban is not null && player.Ban.IsActive,
                    BanReason = player.Ban?.BanReason ?? string.Empty,
                    BanHistory = banHistory
                };

                _logger.LogInformation("{Prefix} Проверка бана для игрока {PlayerId}. Результат: {IsBanned}",
                    prefix, playerId, response.IsBanned);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при проверке бана игрока {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Забанить пользователя
        /// </summary>
        /// <response code="200">Пользователь успешно забанен</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="409">Пользователь уже забанен</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [HttpPost("ban-player")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> BanPlayer([FromBody] BanPlayerRequest request)
        {
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("{Prefix} Некорректные данные запроса для бана игрока", prefix);
                        return BadRequest(ModelState);
                    }

                    // Явная загрузка с отслеживанием изменений
                    var player = await _context.Players
                        .Include(p => p.Ban)
                        .AsTracking()
                        .FirstOrDefaultAsync(p => p.Id == request.PlayerId);

                    if (player is null)
                    {
                        _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, request.PlayerId);
                        return NotFound($"Игрок с ID {request.PlayerId} не найден");
                    }

                    // Проверка бана с учетом параллелизма
                    if (player.Ban is not null)
                    {
                        if (player.Ban.IsActive)
                        {
                            _logger.LogWarning("{Prefix} Игрок {PlayerId} уже забанен", prefix, request.PlayerId);
                            return Conflict("Пользователь уже имеет активный бан");
                        }

                        // Обновление существующего бана
                        player.Ban.IsActive = true;
                        player.Ban.BanReason = request.BanReason;
                        player.Ban.BannedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Создание нового бана
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

                    // Обновление транзакций
                    var transactions = await _context.MoneyTransactions
                        .Where(mt => mt.PlayerId == request.PlayerId &&
                                    mt.TransactionTypeId == 2 &&
                                    mt.TransactionStatusId == 1)
                        .ToListAsync();

                    foreach (var t in transactions)
                    {
                        t.TransactionStatusId = 3;
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("{Prefix} Игрок {PlayerId} забанен", prefix, request.PlayerId);
                    return Ok($"Пользователь #{request.PlayerId} успешно забанен!");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    attempt++;
                    _context.ChangeTracker.Clear();

                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "{Prefix} Ошибка параллелизма при бане игрока {PlayerId} после {Attempt} попыток",
                            prefix, request.PlayerId, attempt);
                        return StatusCode(500, "Не удалось выполнить операцию из-за конфликта данных");
                    }

                    await Task.Delay(100 * attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Prefix} Ошибка при бане игрока {PlayerId}", prefix, request.PlayerId);
                    return StatusCode(500, "Внутренняя ошибка сервера");
                }
            }
            return StatusCode(500);
        }

        /// <summary>
        /// Разбанить пользователя
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Пользователь успешно разбанен</response>
        /// <response code="404">Игрок не найден или не имеет активного бана</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [HttpPost("unban-player/{playerId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UnbanPlayer(int playerId)
        {
            const int maxRetries = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    var player = await _context.Players
                        .Include(p => p.Ban)
                        .AsTracking()
                        .FirstOrDefaultAsync(p => p.Id == playerId);

                    if (player is null)
                    {
                        _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                        return NotFound($"Игрок с ID {playerId} не найден");
                    }

                    if (player.Ban is null || !player.Ban.IsActive)
                    {
                        _logger.LogWarning("{Prefix} Игрок {PlayerId} не имеет активного бана", prefix, playerId);
                        return NotFound("Пользователь не имеет активного бана");
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

                    _logger.LogInformation("{Prefix} Игрок {PlayerId} разбанен", prefix, playerId);
                    return Ok($"Пользователь #{playerId} успешно разбанен!");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    attempt++;
                    _context.ChangeTracker.Clear();

                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "{Prefix} Ошибка параллелизма при разбане игрока {PlayerId} после {Attempt} попыток",
                            prefix, playerId, attempt);
                        return StatusCode(500, "Не удалось выполнить операцию из-за конфликта данных");
                    }

                    await Task.Delay(100 * attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Prefix} Ошибка при разбане игрока {PlayerId}", prefix, playerId);
                    return StatusCode(500, "Внутренняя ошибка сервера");
                }
            }
            return StatusCode(500);
        }

        /// <summary>
        /// Получение списка всех игроков
        /// </summary>
        /// <response code="200">Список пользователей</response>
        /// <response code="404">Игроки не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [HttpGet("get-all-players")]
        [ProducesResponseType(typeof(IEnumerable<PlayerInfoResponse>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<PlayerInfoResponse>>> GetAllPlayers()
        {
            try
            {
                // Проверка существования игроков
                var players = await _context.Players
                        .Include(p => p.GameRounds)
                        .Include(p => p.Balance)
                        .Include(p => p.InvitedByMe)
                        .Include(p => p.Ban)
                        .ToListAsync();

                if (!players.Any())
                {
                    _logger.LogWarning("{Prefix} Игроки не найдены", prefix);
                    return NotFound($"Игроки не найдены");
                }

                var responce = players.Select(p => new PlayerInfoResponse
                {
                    Player = new PlayerDTO
                    {
                        Id = p.Id,
                        TelegramId = p!.TelegramId,
                        Name = p.Name,
                        AvatarUrl = p.AvatarUrl,
                        RegisteredInAppAt = p.RegisteredInAppAt,
                        WinRate = p.WinRate,
                    },
                    Balance = p.Balance.Amount,
                    Rounds = p.GameRounds.Select(t => new RoundDTO
                    {
                        Id = t.Id,
                        Bet = t.Bet,
                        IsWin = t.IsWin,
                        FinishedAt = t.FinishedAt
                    }).ToList(),
                    BannedAt = p.Ban is not null && p.Ban.IsActive ? p.Ban.BannedAt : null,
                    PlayersIdsInvited = p.InvitedByMe.Select(pi => pi.PlayerIdInvited).ToArray(),
                });

                _logger.LogInformation("{Prefix} Получены данные об игроках", prefix);

                // Возврат результата
                return Ok(responce);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении списка всех игроков", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получение данных об игроке
        /// </summary>
        /// <response code="200">Пользователь</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin,Player")]
        [HttpGet("get-player/{playerId:int}")]
        [ProducesResponseType(typeof(PlayerInfoByIdResponse), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PlayerInfoByIdResponse>> GetPlayer([FromRoute] int playerId)
        {
            try
            {
                if (User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                // Проверка существования игрока
                var players = await _context.Players
                        .Include(p => p.GameRounds)
                        .Include(p => p.Balance)
                        .Include(p => p.InvitedByMe)
                        .Include(p => p.Ban)
                        .Include(p => p.PlayerOptions)
                        .ToListAsync();

                var player = players?.FirstOrDefault(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                if (User.IsInRole("Player") && player.Ban is not null && player.Ban.IsActive)
                {
                    _logger.LogWarning("{Prefix} Пользователь забанен", prefix);
                    return Forbid("Пользователь в бане");
                }

                var playersIdsInvited = player.InvitedByMe.Select(i => i.PlayerIdInvited);

                var responce = new PlayerInfoByIdResponse
                {
                    Player = new PlayerDTO
                    {
                        Id = player.Id,
                        TelegramId = player!.TelegramId,
                        Name = player.Name,
                        AvatarUrl = player.AvatarUrl,
                        RegisteredInAppAt = player.RegisteredInAppAt,
                        WinRate = player.WinRate
                    },
                    Balance = player.Balance.Amount,
                    Rounds = player.GameRounds.Select(t => new RoundDTO
                    {
                        Id = t.Id,
                        Bet = t.Bet,
                        IsWin = t.IsWin,
                        FinishedAt = t.FinishedAt
                    }).ToList(),
                    BannedAt = player.Ban is not null && player.Ban.IsActive ? player.Ban.BannedAt : null,
                    Options = new PlayerOptionsDTO
                    {
                        WinChance = player.PlayerOptions.WinChance,
                        Currency = player.PlayerOptions.Currency.ToString(),
                        Wallet = player.PlayerOptions.Wallet,
                        PayoutMethod = player.PlayerOptions.PayoutMethod.ToString(),
                        PaymentMethod = player.PlayerOptions.PaymentMethod.ToString()
                    },
                    PlayersInvited = players!.Where(p => playersIdsInvited.Contains(p.Id)).Select(p => new PlayerDTO
                    {
                        Id = p.Id,
                        TelegramId = p!.TelegramId,
                        Name = p.Name,
                        AvatarUrl = p.AvatarUrl,
                        RegisteredInAppAt = p.RegisteredInAppAt,
                        WinRate = p.WinRate
                    })
                };

                _logger.LogInformation("{Prefix} Получены данные об игроке с PlayerId - {PlayerId}", prefix, playerId);

                // Возврат результата
                return Ok(responce);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении данных об игроке с PlayerId - {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Изменить пользователя
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="request">Модель запроса на изменение игрока</param>
        /// <response code="200">Пользователь успешно изменен</response>
        /// <response code="400">Неккоретные данные запроса</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin, Player")]
        [HttpPost("edit-player/{playerId:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> EditPlayer([FromRoute] int playerId, [FromBody] EditPlayerRequest request)
        {
            try
            {
                // Проверка входных данных
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные запроса для изменения игрока {PlayerId}", prefix, playerId);
                    return BadRequest(ModelState);
                }

                // Проверка существования игрока
                var player = await _context.Players
                                        .Include(p => p.PlayerOptions)
                                        .Include(p => p.Balance)
                                        .FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                // Изменение игрока
                if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                    player.AvatarUrl = request.AvatarUrl!;

                if (!string.IsNullOrWhiteSpace(request.Name))
                    player.Name = request.Name!;

                if (request.WinChance is not null)
                    player.PlayerOptions.WinChance = request.WinChance.Value;

                if (!string.IsNullOrWhiteSpace(request.Currency) && !string.IsNullOrWhiteSpace(request.PaymentMethod))
                {
                    // Проверка, что валюта допустима
                    if (!Enum.TryParse<CurrencyType>(request.Currency, true, out var currency) ||
                        !Enum.IsDefined(typeof(CurrencyType), currency))
                    {
                        _logger.LogWarning("{Prefix} Попытка установить недопустимую валюту: {Currency}", prefix, request.Currency);
                        return BadRequest($"Валюта '{request.Currency}' не поддерживается. Доступные: {string.Join(", ", Enum.GetNames(typeof(CurrencyType)))}");
                    }

                    // Проверка, что метод пополнения допустим
                    if (!Enum.TryParse<PaymentMethodType>(request.PaymentMethod, true, out var method) ||
                        !Enum.IsDefined(typeof(PaymentMethodType), method))
                    {
                        _logger.LogWarning("{Prefix} Попытка установить недопустимую валюту: {PaymentMethod}", prefix, request.PaymentMethod);
                        return BadRequest($"Метод оплаты '{request.PaymentMethod}' не поддерживается. Доступные: {string.Join(", ", Enum.GetNames(typeof(PaymentMethodType)))}");
                    }

                    if (GetCategoryAttributeValue(method) != currency.ToString())
                    {
                        _logger.LogWarning("{Prefix} Попытка установить метод оплаты {PaymentMethod}, не соответсвующий выбранной валюте: {Currency}", prefix, request.PaymentMethod, request.PaymentMethod);
                        return BadRequest($"{prefix} Попытка установить метод оплаты {request.PaymentMethod}, не соответсвующий выбранной валюте: {request.Currency}");
                    }

                    player.PlayerOptions.Currency = currency;
                    player.PlayerOptions.PaymentMethod = method;
                }
                else if ((string.IsNullOrWhiteSpace(request.Currency) && !string.IsNullOrWhiteSpace(request.PaymentMethod)) 
                        || (!string.IsNullOrWhiteSpace(request.Currency) && string.IsNullOrWhiteSpace(request.PaymentMethod)))
                {
                    return BadRequest($"Поля: {nameof(request.Currency)} и {nameof(request.PaymentMethod)} не могут изменятся отдельно друг от друга");
                }

                if (!string.IsNullOrWhiteSpace(request.Wallet) && !string.IsNullOrWhiteSpace(request.PayoutMethod))
                {
                    // Проверка, что метод выплаты допустим
                    if (!Enum.TryParse<PayoutMethodType>(request.PayoutMethod, true, out var payoutMethod) ||
                        !Enum.IsDefined(typeof(PayoutMethodType), payoutMethod))
                    {
                        _logger.LogWarning("{Prefix} Попытка установить недопустимый метод выплаты: {PayoutMethod}", prefix, request.PayoutMethod);
                        return BadRequest($"Метод выплаты '{request.PayoutMethod}' не поддерживается. Доступные: {string.Join(", ", Enum.GetNames(typeof(PayoutMethodType)))}");
                    }
                    player.PlayerOptions.Wallet = request.Wallet!;
                    player.PlayerOptions.PayoutMethod = payoutMethod;
                }
                else if ((string.IsNullOrWhiteSpace(request.Wallet) && !string.IsNullOrWhiteSpace(request.PayoutMethod)) 
                        || (!string.IsNullOrWhiteSpace(request.Wallet) && !string.IsNullOrWhiteSpace(request.PayoutMethod)))
                {
                    return BadRequest($"Поля: {nameof(request.Wallet)} и {nameof(request.PayoutMethod)} не могут изменятся отдельно друг от друга");
                }

                // УБРАТЬ
                if (User.IsInRole("Admin"))
                {
                    if (request.Balance is not null)
                        player.Balance.Amount = request.Balance.Value;
                }

                // Сохранение изменений
                await _context.SaveChangesAsync();

                _logger.LogInformation("{Prefix} Игрок {PlayerId} изменен.", prefix, playerId);

                // Возврат результата
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при изменении игрока {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получение топ 100 списка всех игроков
        /// </summary>
        /// <response code="200">Список пользователей</response>
        /// <response code="404">Игроки не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [HttpGet("get-top100-players")]
        [ProducesResponseType(typeof(PlayerInfoResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<PlayerInfoResponse>>> GetTop100Players()
        {
            try
            {
                // Проверка существования игроков
                var players = await _context.Players
                        .Include(p => p.GameRounds)
                        .Include(p => p.Balance)
                        .Include(p => p.InvitedByMe)
                        .Include(p => p.Ban)
                        .OrderByDescending(p => p.Balance.Amount)
                        .Take(100)
                        .ToListAsync();

                if (!players.Any())
                {
                    _logger.LogWarning("{Prefix} Игроки не найдены", prefix);
                    return NotFound($"Игроки не найдены");
                }

                var responce = players.Select(p => new PlayerInfoResponse
                {
                    Player = new PlayerDTO
                    {
                        Id = p.Id,
                        TelegramId = p!.TelegramId,
                        Name = p.Name,
                        AvatarUrl = p.AvatarUrl,
                        RegisteredInAppAt = p.RegisteredInAppAt,
                        WinRate = p.WinRate,
                    },
                    Balance = p.Balance.Amount,
                    Rounds = p.GameRounds.Select(t => new RoundDTO
                    {
                        Id = t.Id,
                        Bet = t.Bet,
                        IsWin = t.IsWin,
                        FinishedAt = t.FinishedAt
                    }).ToList(),
                    BannedAt = p.Ban is not null && p.Ban.IsActive ? p.Ban.BannedAt : null,
                    PlayersIdsInvited = p.InvitedByMe.Select(pi => pi.PlayerIdInvited).ToArray(),
                });

                _logger.LogInformation("{Prefix} Получены данные о топ-100 игроках", prefix);

                // Возврат результата
                return Ok(responce);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении топ-100 списка всех игроков", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить статистику по всем игрокам
        /// </summary>
        /// <response code="200">Статистика по всем игрокам</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [HttpGet("get-all-players-statistics")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<PlayerStatisticsResponse>>> GetAllPlayersStatistics()
        {
            try
            {
                // Получаем всех игроков с их балансами и раундами
                var players = await _context.Players
                    .Include(p => p.Balance)
                    .Include(p => p.GameRounds)
                    .Include(p => p.Ban)
                    .AsNoTracking()
                    .ToListAsync();

                // Формируем статистику для каждого игрока
                var response = players.Select(p => new PlayerStatisticsResponse
                {
                    Player = new PlayerDTO
                    {
                        Id = p.Id,
                        TelegramId = p!.TelegramId,
                        Name = p.Name,
                        AvatarUrl = p.AvatarUrl,
                        RegisteredInAppAt = p.RegisteredInAppAt,
                        WinRate = p.WinRate,
                    },
                    BannedAt = p.Ban is not null && p.Ban.IsActive ? p.Ban.BannedAt : null,
                    Balance = p.Balance.Amount,
                    RoundsQty = p.GameRounds.Count,
                    WinsQty = p.GameRounds.Count(r => r.IsWin),
                    LossQty = p.GameRounds.Count(r => !r.IsWin),
                    WinAmount = p.GameRounds.Where(r => r.IsWin).Sum(r => r.Bet),
                    LossAmount = p.GameRounds.Where(r => !r.IsWin).Sum(r => r.Bet)
                }).ToList();

                _logger.LogInformation("{Prefix} Получена статистика по {Count} игрокам", prefix, response.Count().ToString());

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении статистики по игрокам", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить статистику по игроку
        /// </summary>
        /// <response code="200">Статистика по игроку</response>
        /// <response code="400">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [HttpGet("get-player-statistics/{playerId:int}")]
        [ProducesResponseType(403)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PlayerStatisticsResponse>> GetPlayerStatistics([FromRoute] int playerId)
        {
            try
            {
                if (User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                // Получаем игрока
                var player = await _context.Players
                    .Include(p => p.Balance)
                    .Include(p => p.GameRounds)
                    .Include(p => p.Ban)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                // Формируем статистику для игрока
                var response = new PlayerStatisticsResponse
                {
                    Player = new PlayerDTO
                    {
                        Id = playerId,
                        TelegramId = player!.TelegramId,
                        Name = player.Name,
                        AvatarUrl = player.AvatarUrl,
                        RegisteredInAppAt = player.RegisteredInAppAt,
                        WinRate = player.WinRate,
                    },
                    BannedAt = player.Ban is not null && player.Ban.IsActive ? player.Ban.BannedAt : null,
                    Balance = player.Balance.Amount,
                    RoundsQty = player.GameRounds.Count,
                    WinsQty = player.GameRounds.Count(r => r.IsWin),
                    LossQty = player.GameRounds.Count(r => !r.IsWin),
                    WinAmount = player.GameRounds.Where(r => r.IsWin).Sum(r => r.Bet),
                    LossAmount = player.GameRounds.Where(r => !r.IsWin).Sum(r => r.Bet)
                };

                _logger.LogInformation("{Prefix} Получена статистика по игроку - {PlayerId}", prefix, playerId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении статистики по игроку", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить статистику сыгранных раундов для вывода по игроку
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Статистика сыгранных раундов для вывода</response>
        /// <response code="404">Игрок или статистика не найдены</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin,Player")]
        [HttpGet("get-player-rounds-statistics-for-withdrawal/{playerId:int}")]
        [ProducesResponseType(typeof(PlayerRoundsStatisticsForWithdrawalDTO), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PlayerRoundsStatisticsForWithdrawalDTO>> GetPlayerRoundsStatisticsForWithdrawal([FromRoute] int playerId)
        {
            try
            {
                if (User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                var player = await _context.Players
                    .Include(p => p.Ban)
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                if (User.IsInRole("Player") && player.Ban is not null && player.Ban.IsActive)
                {
                    _logger.LogWarning("{Prefix} Пользователь забанен", prefix);
                    return Forbid("Пользователь в бане");
                }

                var stat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                    .Include(s => s.MoneyTransaction)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.PlayerId == playerId);

                if (stat is null)
                {
                    _logger.LogWarning("{Prefix} Статистика сыгранных раундов для игрока {PlayerId} не найдена", prefix, playerId);
                    return NotFound($"Статистика сыгранных раундов для игрока {playerId} не найдена");
                }

                var transactionsTypes = await _context.TransactionTypes.ToListAsync();
                var transactionsStatuses = await _context.TransactionStatuses.ToListAsync();

                if (!transactionsStatuses.Any() || !transactionsTypes.Any())
                {
                    _logger.LogWarning("{Prefix} Типы или статусы транзакций не найдены", prefix);
                    return StatusCode(500, "Типы или статусы транзакций не найдены");
                }

                var playerDto = new PlayerDTO
                {
                    Id = player.Id,
                    TelegramId = player.TelegramId,
                    Name = player.Name,
                    AvatarUrl = player.AvatarUrl,
                    RegisteredInAppAt = player.RegisteredInAppAt,
                    WinRate = player.WinRate
                };

                TransactionDTO? lastDeposit = null;
                if (stat.MoneyTransaction is not null)
                {
                    lastDeposit = new TransactionDTO
                    {
                        Id = stat.MoneyTransaction.Id,
                        Amount = stat.MoneyTransaction.Amount,
                        StatusId = stat.MoneyTransaction.TransactionStatusId,
                        StatusName = transactionsStatuses.FirstOrDefault(ts => ts.Id == stat.MoneyTransaction.TransactionStatusId)?.Name,
                        TypeId = stat.MoneyTransaction.TransactionTypeId,
                        TypeName = transactionsTypes.FirstOrDefault(tt => tt.Id == stat.MoneyTransaction.TransactionTypeId)?.Name,
                        CreatedAt = stat.MoneyTransaction.CreatedAt,
                        ProcessedAt = stat.MoneyTransaction.ProcessedAt
                    };
                }

                var dto = new PlayerRoundsStatisticsForWithdrawalDTO
                {
                    Player = playerDto,
                    LastDeposit = lastDeposit,
                    TotalRoundsPlayed = stat.PlayedRoundsCount
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении статистики сыгранных раундов для игрока {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить статистику сыгранных раундов для вывода по всем игрокам
        /// </summary>
        /// <response code="200">Список статистики сыгранных раундов для вывода</response>
        /// <response code="404">Статистика не найдена</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin,Moderator")]
        [HttpGet("get-all-players-rounds-statistics-for-withdrawal")]
        [ProducesResponseType(typeof(IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>> GetAllPlayersRoundsStatisticsForWithdrawal()
        {
            try
            {
                var stats = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                    .Include(s => s.MoneyTransaction)
                    .Include(s => s.Player)
                    .AsNoTracking()
                    .ToListAsync();

                if (!stats.Any())
                {
                    _logger.LogWarning("{Prefix} Статистика сыгранных раундов не найдена", prefix);
                    return NotFound("Статистика сыгранных раундов не найдена");
                }

                var transactionsTypes = await _context.TransactionTypes.ToListAsync();
                var transactionsStatuses = await _context.TransactionStatuses.ToListAsync();

                if (!transactionsStatuses.Any() || !transactionsTypes.Any())
                {
                    _logger.LogWarning("{Prefix} Типы или статусы транзакций не найдены", prefix);
                    return StatusCode(500, "Типы или статусы транзакций не найдены");
                }

                var dtos = stats.Select(stat =>
                {
                    var player = stat.Player;
                    var playerDto = new PlayerDTO
                    {
                        Id = player.Id,
                        TelegramId = player.TelegramId,
                        Name = player.Name,
                        AvatarUrl = player.AvatarUrl,
                        RegisteredInAppAt = player.RegisteredInAppAt,
                        WinRate = player.WinRate
                    };

                    TransactionDTO? lastDeposit = null;
                    if (stat.MoneyTransaction is not null)
                    {
                        lastDeposit = new TransactionDTO
                        {
                            Id = stat.MoneyTransaction.Id,
                            Amount = stat.MoneyTransaction.Amount,
                            StatusId = stat.MoneyTransaction.TransactionStatusId,
                            StatusName = transactionsStatuses.FirstOrDefault(ts => ts.Id == stat.MoneyTransaction.TransactionStatusId)?.Name,
                            TypeId = stat.MoneyTransaction.TransactionTypeId,
                            TypeName = transactionsTypes.FirstOrDefault(tt => tt.Id == stat.MoneyTransaction.TransactionTypeId)?.Name,
                            CreatedAt = stat.MoneyTransaction.CreatedAt,
                            ProcessedAt = stat.MoneyTransaction.ProcessedAt
                        };
                    }

                    return new PlayerRoundsStatisticsForWithdrawalDTO
                    {
                        Player = playerDto,
                        LastDeposit = lastDeposit,
                        TotalRoundsPlayed = stat.PlayedRoundsCount
                    };
                });

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении статистики сыгранных раундов для всех игроков", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        #endregion ActionMethods

        #region Methods

        /// <summary>
        /// Получает значение атрибута категории свойства
        /// </summary>
        public static string GetCategoryAttributeValue(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttribute<CategoryAttribute>();
            return attribute?.Category!;
        }

        #endregion Methods
    }
}
