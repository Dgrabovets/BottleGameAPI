using BottleGame.Application.Common;
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

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Authorization controller
    /// </summary>
    /// <remarks>
    /// Authorizes administrators and players through Telegram
    /// </remarks>
    [ApiController]
    [Route("api/auth")]
    [Tags("Authorization")]
    public class AuthorizationController : Controller
    {
        #region Fields

        /// <inheritdoc cref="ITokenService"/>
        private readonly ITokenService _tokenService;

        /// <inheritdoc cref="ApplicationDbContext"/>
        private readonly ApplicationDbContext _context;

        /// <inheritdoc cref="IPasswordHasher"/>
        private readonly IPasswordHasher _passwordHasher;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<AuthorizationController> _logger;

        /// <inheritdoc cref="ITelegramAuthService"/>
        private readonly ITelegramAuthService _telegramAuthService;

        /// <inheritdoc cref="IPasswordUpgradeService"/>
        private readonly IPasswordUpgradeService _passwordUpgradeService;

        /// <summary>
        /// Prefix of the log messages
        /// </summary>
        const string PREFIX = "[Authorization Controller]";

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizationController"/>
        /// </summary>
        /// <param name="tokenService">Service for working with JWT tokens</param>
        /// <param name="context">Database context</param>
        /// <param name="passwordHasher">Password hashing service</param>
        /// <param name="logger">Logger</param>
        /// <param name="telegramAuthService">Service for authorization through Telegram</param>
        /// <param name="passwordUpgradeService">Service that upgrades old password hashes</param>
        public AuthorizationController(ITokenService tokenService,
                                       ApplicationDbContext context,
                                       IPasswordHasher passwordHasher,
                                       ILogger<AuthorizationController> logger,
                                       ITelegramAuthService telegramAuthService,
                                       IPasswordUpgradeService passwordUpgradeService)
        {
            _tokenService = tokenService;
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _telegramAuthService = telegramAuthService;
            _passwordUpgradeService = passwordUpgradeService;
        }

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
                    _logger.LogWarning("{Prefix} Invalid administrator sign-in data", PREFIX);
                    return BadRequest(ModelState);
                }

                // Look up the administrator in the database
                var admin = await _context.AdminCreds
                    .FirstOrDefaultAsync(a => a.Login == authRequest.Login);

                // An unknown login costs one hash too, so the response time does not tell which logins exist
                if (admin is null)
                {
                    _passwordHasher.HashWithSalt(authRequest.Password);
                }

                // Check the administrator's password
                if (admin is null || !_passwordHasher.Verify(admin.PasswordHash, authRequest.Password, admin.Salt))
                {
                    _logger.LogWarning("{Prefix} Invalid sign-in data for administrator: {Login}", PREFIX, LogSanitizer.Clean(authRequest.Login));
                    return Unauthorized("Invalid login or password");
                }

                await _passwordUpgradeService.UpgradeIfNeededAsync(admin, authRequest.Password);

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

                _logger.LogInformation("{Prefix} Administrator signed in: {Login}", PREFIX, admin.Login);

                // Return the token and the administrator data
                return Ok(new { AdminId = admin.Id, Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error while authorizing the administrator", PREFIX);
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
        [Authorize(Roles = RoleNames.ADMIN)]
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
                    _logger.LogWarning("{Prefix} Invalid administrator registration data", PREFIX);
                    return BadRequest(ModelState);
                }

                if (await _context.AdminCreds.AnyAsync(a => a.Login == authRequest.Login))
                {
                    _logger.LogWarning("{Prefix} The administrator already exists", PREFIX);
                    return Conflict("The administrator already exists");
                }

                var (hash, salt) = _passwordHasher.HashWithSalt(authRequest.Password);

                var admin = new AdministrationRoleUser
                {
                    Login = authRequest.Login,
                    PasswordHash = hash,
                    Salt = salt,
                    CreatedAt = DateTime.UtcNow,
                    Role = AdministrationRoles.ADMIN,
                };

                _context.AdminCreds.Add(admin);
                await _context.SaveChangesAsync();

                _logger.LogInformation("{Prefix} A new administrator was registered: {Login}", PREFIX, LogSanitizer.Clean(authRequest.Login));

                return Ok(new { Message = "The administrator was registered" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error while registering the administrator", PREFIX);
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
                    _logger.LogWarning("{Prefix} Empty Telegram InitData", PREFIX);
                    return BadRequest("Invalid Telegram initialization data");
                }

                // Check that the Telegram data is authentic, on the string exactly as Telegram signed it
                if (!_telegramAuthService.ValidateInitData(initData))
                {
                    _logger.LogWarning("{Prefix} Unverified Telegram data", PREFIX);
                    return Unauthorized("Invalid Telegram authorization data");
                }

                ParsedTelegramUser playerData;
                try
                {
                    playerData = _telegramAuthService.ParseInitData(initData);
                }
                catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or JsonException
                                              or KeyNotFoundException or NullReferenceException or IndexOutOfRangeException)
                {
                    // Signed data can still miss a field the sign-in needs
                    _logger.LogWarning("{Prefix} Malformed Telegram initData", PREFIX);
                    return Unauthorized("Invalid Telegram authorization data");
                }

                // Find or create the player in the database
                var player = await _context.Players.Include(p => p.Ban).FirstOrDefaultAsync(p => p.TelegramId == playerData.TelegramId);

                if (player?.Ban is not null && player?.Ban.IsActive is true)
                {
                    _logger.LogWarning("{Prefix} The player is banned", PREFIX);
                    return StatusCode(403, "The player is banned");
                }

                if (player is null)
                {
                    var gameParams = await _context.GameParams.FirstOrDefaultAsync();

                    // The inviter counts only if they exist, have a referral link and are not banned, otherwise the invitation is ignored
                    var inviter = playerData.InvitingPlayerId is { } inviterId
                                    ? await _context.Players.FirstOrDefaultAsync(p => p.Id == inviterId && p.ReferralLink != null && (p.Ban == null || !p.Ban.IsActive))
                                    : null;

                    if (playerData.InvitingPlayerId is not null && inviter is null)
                    {
                        _logger.LogWarning("{Prefix} The invitation from player {PlayerId} is ignored: no such player with a referral link", PREFIX, playerData.InvitingPlayerId);
                    }

                    // Create a new player
                    player = new Player
                    {
                        TelegramId = playerData.TelegramId,
                        Name = playerData.Username,
                        AvatarUrl = playerData.PhotoUrl,
                        RegisteredInAppAt = DateTime.UtcNow
                    };

                    var roundsStatisticsForWithdraw = new PlayerPlayedRoundsStatisticForWithdrawal
                    {
                        Player = player,
                        DepositTransactionId = null,
                        MoneyTransaction = null,
                        PlayedRoundsCount = 0
                    };

                    var balance = new Balance
                    {
                        Player = player,
                        Amount = inviter is null ? 0 : 100
                    };

                    var options = new PlayerOptions
                    {
                        Player = player,
                        WinChance = gameParams!.BaseWinChance,
                        Currency = "rub",
                        Wallet = string.Empty,
                        PayoutMethod = "sberbank",
                        PaymentMethod = "sberbank"
                    };

                    _context.Players.Add(player);
                    _context.PlayersBalances.Add(balance);
                    _context.PlayersOptions.Add(options);
                    _context.PlayersPlayedRoundsStatisticForWithdrawals.Add(roundsStatisticsForWithdraw);

                    if (inviter is not null)
                    {
                        _context.InvitedPlayers.Add(new InvitedPlayer
                        {
                            Inviter = inviter,
                            Invitee = player,
                            InvitedAt = DateTime.UtcNow
                        });
                    }

                    // One save for the whole account, so a failure leaves nothing half-created
                    await _context.SaveChangesAsync();
                }

                // Generate the JWT token
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, player.Id.ToString()),
                    new(ClaimTypes.Name, player.Name),
                    new(ClaimTypes.Role, RoleNames.PLAYER),
                    new("TelegramId", player.TelegramId.ToString()),
                    new("LoginTime", DateTime.UtcNow.ToString())
                };

                var token = _tokenService.GenerateToken(claims);

                _logger.LogInformation("{Prefix} Telegram player authorized: {TelegramId}", PREFIX, player.TelegramId);

                // Return the result
                return Ok(new AuthResponse { PlayerId = player.Id, Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error while authorizing the Telegram player", PREFIX);
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion ActionMethods
    }
}
