using BottleGame.Application.DTOs;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Web;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер авторизации
    /// </summary>
    /// <remarks>
    /// Обеспечивает авторизацию администраторов и игроков через Telegram
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="AuthorizationController"/>.
    /// </remarks>
    /// <param name="tokenService">Сервис для работы с JWT токенами.</param>
    /// <param name="context">Контекст базы данных.</param>
    /// <param name="passwordHasher">Сервис для хеширования паролей.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="telegramAuthService">Сервис для авторизации через Telegram.</param>
    [ApiController]
    [Route("api/auth")]
    [Tags("Авторизация")]
    public class AuthorizationController(ITokenService tokenService, ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<AuthorizationController> logger, ITelegramAuthService telegramAuthService) : Controller
    {
        #region Fields

        private readonly ITokenService _tokenService = tokenService;
        private readonly ApplicationDbContext _context = context;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly ILogger<AuthorizationController> _logger = logger;
        private readonly ITelegramAuthService _telegramAuthService = telegramAuthService;

        const string prefix = "[Authorization Controller]";

        #endregion Fields
        #region Constructor

        #endregion Constructor

        #region ActionMethods

        /// <summary>
        /// Авторизация администратора
        /// </summary>
        /// <param name="authRequest">Данные для входа (логин и пароль)</param>
        /// <response code="200">Успешная авторизация, возвращает JWT токен</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="401">Неверные данные входа</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("admin-login")]
        public async Task<ActionResult<AuthResponse>> LoginAdmin([FromBody] AdministrationRoleUserCredentials authRequest)
        {
            try
            {
                // Проверка валидности модели
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные для входа администратора", prefix);
                    return BadRequest(ModelState);
                }

                // Поиск администратора в базе
                var admin = await _context.AdminCreds
                    .FirstOrDefaultAsync(a => a.Login == authRequest.Login);

                // Проверка пароля администратора
                if (admin is null || !_passwordHasher.Verify(admin.PasswordHash, authRequest.Password, admin.Salt))
                {
                    _logger.LogWarning("{Prefix} Неверные данные входа для администратора: {Login}", prefix, authRequest.Login);
                    return Unauthorized("Неверный логин или пароль");
                }

                // Формирование claims для токена
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                    new(ClaimTypes.Name, admin.Login),
                    new(ClaimTypes.Role, AdministrationRoles.GetRoleName(admin.Role)),
                    new("LoginTime", DateTime.UtcNow.ToString())
                };

                // Генерация JWT токена
                var token = _tokenService.GenerateToken(claims);

                _logger.LogInformation("{Prefix} Успешный вход администратора: {Login}", prefix, admin.Login);

                // Возврат токена и данных администратора
                return Ok(new { AdminId = admin.Id, Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при авторизации администратора", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Авторизация игрока через Telegram Mini App
        /// </summary>
        /// <param name="initData">Данные инициализации Telegram WebApp</param>
        /// <response code="200">Успешная авторизация, возвращает JWT токен</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="401">Неверные данные Telegram</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("player-login/{initData}")]
        public async Task<ActionResult<AuthResponse>> TelegramLogin([FromRoute] string initData)
        {
            try
            {
                // Проверка валидности модели
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Пустые данные Telegram InitData", prefix);
                    return BadRequest("Некорректные данные инициализации Telegram");
                }

                var playerData = Parse(initData);

                bool isReferalLinkIsNotExisting = await _context.ReferralLinks.FirstOrDefaultAsync(p => p.PlayerId == playerData.InvitingPlayerId) is null;

                if (isReferalLinkIsNotExisting)
                {
                    var query = HttpUtility.ParseQueryString(initData);

                    query.Remove("start_param");
                    initData = query.ToString()!;
                }

                // Проверка подлинности данных Telegram
                if (!_telegramAuthService.ValidateInitData(initData))
                {
                    _logger.LogWarning("{Prefix} Неподтверждённые данные Telegram", prefix);
                    return Unauthorized("Неверные данные авторизации Telegram");
                }

                // Поиск или создание игрока в БД
                var player = await _context.Players.Include(p => p.Ban).FirstOrDefaultAsync(p => p.TelegramId == playerData.TelegramId);

                if (player?.Ban is not null && player?.Ban.IsActive is true)
                {
                    _logger.LogWarning("{Prefix} Игрок забанен", prefix);
                    return Forbid("Игрок находится в бане");
                }

                if (player is null)
                {
                    var gameParams = await _context.GameParams.FirstOrDefaultAsync();

                    // Создание нового игрока
                    player = new Player
                    {
                        TelegramId = playerData.TelegramId,
                        Name = playerData.Username,
                        AvatarUrl = playerData.PhotoUrl,
                        RegisteredInAppAt = DateTime.UtcNow
                    };

                    _context.Players.Add(player);
                    await _context.SaveChangesAsync();

                    var roundsStatisticsForWithdraw = new PlayerPlayedRoundsStatisticForWithdrawal
                    {
                        Player = player,
                        PlayerId = player.Id,
                        DepositTransactionId = null,
                        MoneyTransaction = null,
                        PlayedRoundsCount = 0
                    };

                    var balance = new Balance
                    {
                        Player = player,
                        PlayerId = player.Id,
                        Amount = 0
                    };

                    var options = new PlayerOptions
                    {
                        Player = player,
                        PlayerId = player.Id,
                        WinChance = gameParams!.BaseWinChance,
                        Currency = "rub",
                        Wallet = string.Empty,
                        PayoutMethod = "sberbank",
                        PaymentMethod = "sberbank"
                    };

                    _context.PlayersOptions.Add(options);
                    _context.PlayersPlayedRoundsStatisticForWithdrawals.Add(roundsStatisticsForWithdraw);

                    if (playerData.InvitingPlayerId is not null)
                    {
                        var wasPlayerInvitedBefore = await _context.InvitedPlayers.AnyAsync(ip => ip.PlayerIdInvited == player.Id);

                        if (!wasPlayerInvitedBefore)
                        {
                            var playerInvitedBy = await _context.Players.Include(p => p.Ban).FirstOrDefaultAsync(p => p.Id == playerData.InvitingPlayerId);

                            if (playerInvitedBy is null)
                            {
                                _logger.LogWarning("{Prefix} Игрок (пригласивший) с ID {PlayerId} не найден", prefix, playerData.InvitingPlayerId);
                                return NotFound($"Игрок (пригласивший) с ID {playerData.InvitingPlayerId} не найден");
                            }

                            if (playerInvitedBy.Id != player.Id && (playerInvitedBy.Ban is null || !playerInvitedBy.Ban.IsActive))
                            {
                                var invitedPlayer = new InvitedPlayer
                                {
                                    PlayerInvited = playerInvitedBy,
                                    PlayerInvitedBy = player,
                                    PlayerIdInvited = playerInvitedBy.Id,
                                    PlayerIdInvitedBy = playerData.InvitingPlayerId.Value,
                                    InvitedAt = DateTime.UtcNow
                                };

                                balance.Amount = 100;

                                _context.InvitedPlayers.Add(invitedPlayer);
                            }
                        }
                    }

                    _context.PlayersBalances.Add(balance);
                    await _context.SaveChangesAsync();
                }

                // Генерация JWT токена
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, player.Id.ToString()),
                    new(ClaimTypes.Name, player.Name),
                    new(ClaimTypes.Role, "Player"),
                    new("TelegramId", player.TelegramId.ToString()),
                    new("LoginTime", DateTime.UtcNow.ToString())
                };

                var token = _tokenService.GenerateToken(claims);

                _logger.LogInformation("{Prefix} Успешная авторизация Telegram игрока: {TelegramId}", prefix, player.TelegramId);

                // Возврат результата
                return Ok(new AuthResponse { PlayerId = player.Id, Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка авторизации Telegram игрока", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        #endregion ActionMethods

        #region Methods

        /// <summary>
        /// Парсит данные пользователя Telegram из initData
        /// </summary>
        /// <param name="initData">Строка инициализации Telegram WebApp</param>
        /// <returns>Распарсенные данные пользователя Telegram</returns>
        private static ParsedTelegramUser Parse(string initData)
        {
            var result = new ParsedTelegramUser();
            var query = HttpUtility.ParseQueryString(initData);

            // Извлечение данных пользователя из query string
            result.TelegramId = long.Parse(GetJsonValue(query["user"]!, "id")!);
            result.Username = GetJsonValue(query["user"]!, "username") ?? GetJsonValue(query["user"]!, "first_name")!;
            result.PhotoUrl = GetJsonValue(query["user"]!, "photo_url") ?? string.Empty;
            var startParam = query["start_param"]?.Split("_")[1] ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(startParam))
            {
                result.InvitingPlayerId = Convert.ToInt32(startParam);
            }

            // Парсинг даты (Unix timestamp)
            result.RegisteredAt = DateTimeOffset.FromUnixTimeSeconds(
                long.Parse(query["auth_date"]!)
            ).UtcDateTime;

            return result;
        }

        /// <summary>
        /// Вспомогательный метод для извлечения значений из JSON-строки
        /// </summary>
        /// <param name="json">JSON-строка</param>
        /// <param name="propertyName">Имя свойства</param>
        /// <returns>Значение свойства или null</returns>
        private static string? GetJsonValue(string json, string propertyName)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString(),
                    JsonValueKind.Number => property.GetInt64().ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => null
                };
            }
            return null;
        }

        #endregion Methods
    }
}

