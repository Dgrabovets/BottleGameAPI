using BottleGameServer.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using BottleGameServer.Authorization;
using BottleGameServer.Authorization.Models;
using Microsoft.EntityFrameworkCore;
using BottleGameServer.Models.Entities;
using BottleGameServer.Models.DTOs.Requests;
using BottleGameServer.Models.DTOs.Responses;
using System.Text.Json;
using System.Web;

namespace BottleGameServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        #region Fields

        private readonly ITokenService _tokenService;
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthController> _logger;
        private readonly ITelegramAuthService _telegramAuthService;

        const string prefix = "[Auth Controller]";

        #endregion Fields

        #region Constructor

        public AuthController(ITokenService tokenService, ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<AuthController> logger, ITelegramAuthService telegramAuthService)
        {
            _tokenService = tokenService;
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _telegramAuthService = telegramAuthService;
        }

        #endregion Constructor

        #region ActionMethods

        /// <summary>
        /// Аутентификация администратора
        /// </summary>
        /// <param name="authRequest">Данные для входа (логин и пароль)</param>
        /// <response code="200">Успешная аутентификация, возвращает JWT токен</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="401">Неверные учетные данные</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [HttpPost("admin-login")]
        public async Task<ActionResult<AuthResponse>> LoginAdmin([FromBody] AuthAdminRequest authRequest)
        {
            try
            {
                // Проверка валидации модели
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные для входа администратора", prefix);
                    return BadRequest(ModelState);
                }

                // Поиск администратора в базе
                var admin = await _context.AdminCreds
                    .FirstOrDefaultAsync(a => a.Login == authRequest.Login);

                // Проверка учетных данных
                if (admin == null || !_passwordHasher.Verify(admin.PasswordHash, authRequest.Password, admin.Salt))
                {
                    _logger.LogWarning("{Prefix} Неудачная попытка входа для администратора: {Login}", prefix, authRequest.Login);
                    return Unauthorized("Неверный логин или пароль");
                }

                // Формирование claims для токена
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                    new Claim(ClaimTypes.Name, admin.Login),
                    new Claim(ClaimTypes.Role, admin.Role == 1 ? "Admin" : "Moderator"),
                    new Claim("LoginTime", DateTime.UtcNow.ToString())
                };

                // Генерация JWT токена
                var token = _tokenService.GenerateToken(claims);

                _logger.LogInformation("{Prefix} Успешный вход администратора: {Login}", prefix, admin.Login);

                // Возвращаем токен и основную информацию
                return Ok(new { AdminId = admin.Id, Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при аутентификации администратора", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Аутентификация пользователя Telegram через Mini App
        /// </summary>
        /// <param name="initData">Данные инициализации Telegram WebApp</param>
        /// <response code="200">Успешная аутентификация, возвращает JWT токен</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="401">Неверные данные Telegram</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        [HttpPost("player-login/{initData}")]
        public async Task<ActionResult<AuthResponse>> TelegramLogin([FromRoute] string initData)
        {
            try
            {
                // Проверка валидации модели
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Пустые данные Telegram InitData", prefix);
                    return BadRequest("Требуются данные инициализации Telegram");
                }

                // Проверка подлинности данных Telegram
                if (!_telegramAuthService.ValidateInitData(initData))
                {
                    _logger.LogWarning("{Prefix} Невалидные данные Telegram", prefix);
                    return Unauthorized("Неверные данные аутентификации Telegram");
                }

                var playerData = _parse(initData);

                // Поиск или создание пользователя в БД
                var player = await _context.Players.Include(p => p.Ban).FirstOrDefaultAsync(p => p.TelegramId == playerData.TelegramId);

                if (player?.Ban is not null && player?.Ban.IsActive is true)
                {
                    _logger.LogWarning("{Prefix} Пользователь забанен", prefix);
                    return Forbid("Пользователь в бане");
                }

                if (player is null)
                {
                    var gameParams = await _context.GameParams.FirstOrDefaultAsync();

                    // Создаем нового игрока
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
                        PlayerId = player.Id, // Используем автоинкрементный Id
                        Amount = 0
                    };

                    var options = new PlayerOptions
                    {
                        Player = player,
                        PlayerId = player.Id,
                        WinChance = gameParams!.BaseWinChance
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
                                _logger.LogWarning("{Prefix} Игрок (приглашаюший) с ID {PlayerId} не найден", prefix, playerInvitedBy!.Id);
                                return NotFound($"Игрок (приглашаюший) с ID {playerInvitedBy.Id} не найден");
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
                    new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
                    new Claim(ClaimTypes.Name, player.Name),
                    new Claim(ClaimTypes.Role, "Player"),
                    new Claim("TelegramId", player.TelegramId.ToString()),
                    new Claim("LoginTime", DateTime.UtcNow.ToString())
                };

                var token = _tokenService.GenerateToken(claims);

                _logger.LogInformation("{Prefix} Успешная аутентификация Telegram пользователя: {TelegramId}", prefix, player.TelegramId);

                // Возврат токена
                return Ok(new AuthResponse { PlayerId = player.Id, Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка аутентификации Telegram пользователя", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        #endregion ActionMethods

        #region Methods

        private ParsedResult _parse(string initData)
        {
            var result = new ParsedResult();
            var query = HttpUtility.ParseQueryString(initData);

            // Извлекаем данные пользователя напрямую из query string
            result.TelegramId = long.Parse(GetJsonValue(query["user"]!, "id")!);
            result.Username = GetJsonValue(query["user"]!, "username") ?? GetJsonValue(query["user"]!, "first_name")!;
            result.PhotoUrl = GetJsonValue(query["user"]!, "photo_url") ?? string.Empty;
            var startParam = query["start_param"]?.Split("_")[1] ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(startParam))
            {
                result.InvitingPlayerId = Convert.ToInt32(startParam);
            }

            // Парсим дату (Unix timestamp)
            result.RegisteredAt = DateTimeOffset.FromUnixTimeSeconds(
                long.Parse(query["auth_date"]!)
            ).UtcDateTime;

            return result;
        }

        // Вспомогательный метод для извлечения значений из JSON-строки
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
