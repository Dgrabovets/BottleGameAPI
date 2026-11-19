using BottleGame.Application.DTOs;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Web;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Authorization controller
    /// </summary>
    /// <remarks>
    /// Authorizes administrators and players through Telegram
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of <see cref="AuthorizationController"/>
    /// </remarks>
    /// <param name="tokenService">Service for working with JWT tokens</param>
    /// <param name="context">Database context</param>
    /// <param name="passwordHasher">Password hashing service</param>
    /// <param name="logger">Logger</param>
    /// <param name="telegramAuthService">Service for authorization through Telegram</param>
    [ApiController]
    [Route("api/auth")]
    [Tags("Authorization")]
    public class AuthorizationController(ITokenService tokenService, ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<AuthorizationController> logger, ITelegramAuthService telegramAuthService) : Controller
    {
        #region Fields

        /// <inheritdoc cref="ITokenService"/>
        private readonly ITokenService _tokenService = tokenService;

        /// <inheritdoc cref="ApplicationDbContext"/>
        private readonly ApplicationDbContext _context = context;

        /// <inheritdoc cref="IPasswordHasher"/>
        private readonly IPasswordHasher _passwordHasher = passwordHasher;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<AuthorizationController> _logger = logger;

        /// <inheritdoc cref="ITelegramAuthService"/>
        private readonly ITelegramAuthService _telegramAuthService = telegramAuthService;

        /// <summary>
        /// Prefix of the log messages
        /// </summary>
        const string prefix = "[Authorization Controller]";

        #endregion Fields

        #region ActionMethods

        /// <summary>
        /// Authorizes an administrator
        /// </summary>
        /// <param name="authRequest">Sign-in data (login and password)</param>
        /// <response code="200">Authorization succeeded, returns a JWT token</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid sign-in data</response>
        /// <response code="500">Internal server error</response>
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("auth")]
        [HttpPost("admin-login")]
        public async Task<ActionResult<AuthResponse>> LoginAdmin([FromBody] AdministrationRoleUserCredentials authRequest)
        {
            try
            {
                // Check that the model is valid
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Invalid administrator sign-in data", prefix);
                    return BadRequest(ModelState);
                }

                // Look up the administrator in the database
                var admin = await _context.AdminCreds
                    .FirstOrDefaultAsync(a => a.Login == authRequest.Login);

                // Check the administrator's password
                if (admin is null || !_passwordHasher.Verify(admin.PasswordHash, authRequest.Password, admin.Salt))
                {
                    _logger.LogWarning("{Prefix} Invalid sign-in data for administrator: {Login}", prefix, authRequest.Login);
                    return Unauthorized("Invalid login or password");
                }

                await UpgradePasswordHashAsync(admin, authRequest.Password);

                // Build the claims for the token
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                    new(ClaimTypes.Name, admin.Login),
                    new(ClaimTypes.Role, AdministrationRoles.GetRoleName(admin.Role)),
                    new("LoginTime", DateTime.UtcNow.ToString())
                };

                // Generate the JWT token
                var token = _tokenService.GenerateToken(claims);

                _logger.LogInformation("{Prefix} Administrator signed in: {Login}", prefix, admin.Login);

                // Return the token and the administrator data
                return Ok(new { AdminId = admin.Id, Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error while authorizing the administrator", prefix);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Registers a new administrator
        /// </summary>
        /// <param name="authRequest">Registration data (login and password)</param>
        /// <response code="200">The administrator was registered</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="409">The administrator already exists</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("auth")]
        [HttpPost("admin-register")]
        public async Task<IActionResult> RegisterAdmin([FromBody] AdministrationRoleUserCredentials authRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Invalid administrator registration data", prefix);
                    return BadRequest(ModelState);
                }

                if (await _context.AdminCreds.AnyAsync(a => a.Login == authRequest.Login))
                {
                    _logger.LogWarning("{Prefix} The administrator already exists", prefix);
                    return Conflict("The administrator already exists");
                }

                var (hash, salt) = _passwordHasher.HashWithSalt(authRequest.Password);

                var admin = new AdministrationRoleUser
                {
                    Login = authRequest.Login,
                    PasswordHash = hash,
                    Salt = salt,
                    CreatedAt = DateTime.UtcNow,
                    Role = AdministrationRoles.Admin,
                };

                _context.AdminCreds.Add(admin);
                await _context.SaveChangesAsync();

                _logger.LogInformation("{Prefix} A new administrator was registered: {Login}", prefix, authRequest.Login);

                return Ok(new { Message = "The administrator was registered" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error while registering the administrator", prefix);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Authorizes a player through the Telegram Mini App
        /// </summary>
        /// <param name="initData">Telegram WebApp initialization data</param>
        /// <response code="200">Authorization succeeded, returns a JWT token</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid Telegram data</response>
        /// <response code="403">The player is banned</response>
        /// <response code="500">Internal server error</response>
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("auth")]
        [HttpPost("player-login/{initData}")]
        public async Task<ActionResult<AuthResponse>> TelegramLogin([FromRoute] string initData)
        {
            try
            {
                // Check that the model is valid
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Empty Telegram InitData", prefix);
                    return BadRequest("Invalid Telegram initialization data");
                }

                ParsedTelegramUser playerData;
                try
                {
                    playerData = Parse(initData);
                }
                catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or JsonException
                                              or KeyNotFoundException or NullReferenceException or IndexOutOfRangeException)
                {
                    // The data is not signed yet at this point, so any garbage can arrive here
                    _logger.LogWarning("{Prefix} Malformed Telegram initData", prefix);
                    return Unauthorized("Invalid Telegram authorization data");
                }

                bool isReferalLinkIsNotExisting = await _context.ReferralLinks.FirstOrDefaultAsync(p => p.PlayerId == playerData.InvitingPlayerId) is null;

                if (isReferalLinkIsNotExisting)
                {
                    var query = HttpUtility.ParseQueryString(initData);

                    query.Remove("start_param");
                    initData = query.ToString()!;
                }

                // Check that the Telegram data is authentic
                if (!_telegramAuthService.ValidateInitData(initData))
                {
                    _logger.LogWarning("{Prefix} Unverified Telegram data", prefix);
                    return Unauthorized("Invalid Telegram authorization data");
                }

                // Find or create the player in the database
                var player = await _context.Players.Include(p => p.Ban).FirstOrDefaultAsync(p => p.TelegramId == playerData.TelegramId);

                if (player?.Ban is not null && player?.Ban.IsActive is true)
                {
                    _logger.LogWarning("{Prefix} The player is banned", prefix);
                    return Forbid("The player is banned");
                }

                if (player is null)
                {
                    var gameParams = await _context.GameParams.FirstOrDefaultAsync();

                    // Create a new player
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
                                _logger.LogWarning("{Prefix} The inviting player with id {PlayerId} was not found", prefix, playerData.InvitingPlayerId);
                                return NotFound($"The inviting player with id {playerData.InvitingPlayerId} was not found");
                            }

                            if (playerInvitedBy.Id != player.Id && (playerInvitedBy.Ban is null || !playerInvitedBy.Ban.IsActive))
                            {
                                var invitedPlayer = new InvitedPlayer
                                {
                                    Inviter = playerInvitedBy,
                                    Invitee = player,
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

                // Generate the JWT token
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, player.Id.ToString()),
                    new(ClaimTypes.Name, player.Name),
                    new(ClaimTypes.Role, "Player"),
                    new("TelegramId", player.TelegramId.ToString()),
                    new("LoginTime", DateTime.UtcNow.ToString())
                };

                var token = _tokenService.GenerateToken(claims);

                _logger.LogInformation("{Prefix} Telegram player authorized: {TelegramId}", prefix, player.TelegramId);

                // Return the result
                return Ok(new AuthResponse { PlayerId = player.Id, Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error while authorizing the Telegram player", prefix);
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion ActionMethods

        #region Methods

        /// <summary>
        /// Replaces a hash of an old format with a current one, now that the password is known to be right.
        /// A failure to save does not fail the sign-in: the old hash still works
        /// </summary>
        /// <param name="admin">Administrator who just signed in</param>
        /// <param name="password">The password that was just checked</param>
        private async Task UpgradePasswordHashAsync(AdministrationRoleUser admin, string password)
        {
            if (!_passwordHasher.NeedsRehash(admin.PasswordHash))
            {
                return;
            }

            try
            {
                var (hash, salt) = _passwordHasher.HashWithSalt(password);
                admin.PasswordHash = hash;
                admin.Salt = salt;
                await _context.SaveChangesAsync();
                _logger.LogInformation("{Prefix} Password hash of {Login} upgraded", prefix, admin.Login);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Prefix} Could not upgrade the password hash of {Login}", prefix, admin.Login);
            }
        }

        /// <summary>
        /// Parses the Telegram user data from initData
        /// </summary>
        /// <param name="initData">Telegram WebApp initialization string</param>
        /// <returns>Parsed Telegram user data</returns>
        private static ParsedTelegramUser Parse(string initData)
        {
            var result = new ParsedTelegramUser();
            var query = HttpUtility.ParseQueryString(initData);

            // Extract the user data from the query string
            result.TelegramId = long.Parse(GetJsonValue(query["user"]!, "id")!);
            result.Username = GetJsonValue(query["user"]!, "username") ?? GetJsonValue(query["user"]!, "first_name")!;
            result.PhotoUrl = GetJsonValue(query["user"]!, "photo_url") ?? string.Empty;
            var startParts = query["start_param"]?.Split('_');

            if (startParts is { Length: > 1 } && int.TryParse(startParts[1], out var invitingPlayerId))
            {
                result.InvitingPlayerId = invitingPlayerId;
            }

            // Parse the date (Unix timestamp)
            result.RegisteredAt = DateTimeOffset.FromUnixTimeSeconds(
                long.Parse(query["auth_date"]!)
            ).UtcDateTime;

            return result;
        }

        /// <summary>
        /// Helper method that extracts values from a JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>The property value or null</returns>
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
