using BottleGame.API.Extensions;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Referral system controller
    /// </summary>
    /// <remarks>
    /// Manages referral links and invitations
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Referral system")]
    public class ReferralController : Controller
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<ReferralController> _logger;

        /// <summary>
        /// Name of the application used to build referral links
        /// </summary>
        private readonly string _appName;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="ReferralController"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger</param>
        /// <param name="configuration">Application configuration</param>
        public ReferralController(ApplicationDbContext context, ILogger<ReferralController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _appName = configuration["AppName"]!;
        }

        #region ActionMethods

        /// <summary>
        /// Gets the referral link of a player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <response code="200">Referral link</response>
        /// <response code="403">The player is banned</response>
        /// <response code="404">The player was not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.ADMIN_OR_PLAYER)]
        [ProducesResponseType(typeof(GetReferralLinkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-referral-link/{playerId:int}")]
        public async Task<ActionResult<GetReferralLinkResponse>> GetReferralLink([FromRoute] int playerId)
        {
            try
            {
                if (User.IsInRole(RoleNames.PLAYER) && User.GetUserId() != playerId)
                {
                    return Unauthorized();
                }

                // Find the player
                var player = await _context.Players.Include(p => p.Ban).Include(p => p.ReferralLink).FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("Player with id {PlayerId} not found", playerId);
                    return NotFound($"Player with id {playerId} not found");
                }

                if (User.IsInRole(RoleNames.PLAYER) && player.Ban is not null && player.Ban.IsActive)
                {
                    _logger.LogWarning("The player is banned");
                    return StatusCode(403, "The player is banned");
                }

                var referralLink = player.ReferralLink;

                // Create the referral link
                if (referralLink is null)
                {
                    referralLink = new ReferralLink
                    {
                        PlayerId = playerId,
                        Link = $"https://t.me/{_appName}?startapp=ref_{playerId}"
                    };

                    _context.ReferralLinks.Add(referralLink);

                    await _context.SaveChangesAsync();
                }

                // Return the link
                _logger.LogInformation("Link received for player {PlayerId}", playerId);

                return Ok(new GetReferralLinkResponse { ReferralLink = referralLink.Link });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting the referral link for player {PlayerId}", playerId);
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion ActionMethods
    }
}
