using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Player management controller
    /// </summary>
    /// <remarks>
    /// Manages player data, bans, statistics and profiles
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of <see cref="PlayerController"/>
    /// </remarks>
    /// <param name="playerService">Service for working with players</param>
    /// <param name="playerStatisticsService">Player statistics service</param>
    /// <param name="banService">Service for working with bans</param>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Player management")]
    public class PlayerController(IPlayerService playerService, IPlayerStatisticsService playerStatisticsService, IBanService banService) : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Service for working with players
        /// </summary>
        private readonly IPlayerService _playerService = playerService;

        /// <summary>
        /// Player statistics service
        /// </summary>
        private readonly IPlayerStatisticsService _playerStatisticsService = playerStatisticsService;

        /// <summary>
        /// Service for working with player bans
        /// </summary>
        private readonly IBanService _banService = banService;

        #endregion Fields

        #region Ban Methods

        /// <summary>
        /// Checks whether a user is banned
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <response code="200">Ban information</response>
        /// <response code="404">The player was not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.All)]
        [HttpGet("is-banned/{playerId:int}")]
        [ProducesResponseType(typeof(IsBannedResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IsBannedResponse>> IsBanned([FromRoute] int playerId)
        {
            if (!HasAccessToPlayer(playerId))
            {
                return Unauthorized();
            }

            var result = await _playerService.IsBanned(playerId);
            return ToActionResult(result);
        }

        /// <summary>
        /// Bans a user
        /// </summary>
        /// <response code="200">The user was banned</response>
        /// <response code="404">The player was not found</response>
        /// <response code="409">The user is already banned</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost("ban-player")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BanPlayer([FromBody] BanPlayerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _banService.BanPlayer(request);

            if (result.IsSuccess)
            {
                return Ok($"User #{request.PlayerId} was banned");
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Unbans a user
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <response code="200">The user was unbanned</response>
        /// <response code="404">The player was not found or has no active ban</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost("unban-player/{playerId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UnbanPlayer(int playerId)
        {
            var result = await _banService.UnbanPlayer(playerId);

            if (result.IsSuccess)
            {
                return Ok($"User #{playerId} was unbanned");
            }

            return ToActionResult(result);
        }

        #endregion Ban Methods

        #region Player Data Methods

        /// <summary>
        /// Gets the list of all players
        /// </summary>
        /// <param name="telegramId">Exact Telegram id (search, includes banned players)</param>
        /// <param name="name">Part of the nickname (search, includes banned players)</param>
        /// <param name="q">One search field: a Telegram id or part of the nickname</param>
        /// <param name="page">Page number from 1; with page or pageSize the answer holds one page and X-Total-Count carries the total</param>
        /// <param name="pageSize">Players per page, at most 200, 20 by default</param>
        /// <response code="200">List of users</response>
        /// <response code="404">No players were found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-all-players")]
        [ProducesResponseType(typeof(IEnumerable<PlayerInfoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PlayerInfoResponse>>> GetAllPlayers([FromQuery] long? telegramId = null,
                                                                                       [FromQuery] string? name = null,
                                                                                       [FromQuery] string? q = null,
                                                                                       [FromQuery] int? page = null,
                                                                                       [FromQuery] int? pageSize = null)
        {
            var search = PlayerSearchQuery.Resolve(telegramId, name, q);
            var pageRequest = PageRequest.Create(page, pageSize);
            var result = await _playerService.GetAllPlayers(search.TelegramId, search.Name, pageRequest);

            if (pageRequest is not null)
            {
                Response.Headers[TotalCountHeader] = (await _playerService.CountPlayers(search.TelegramId, search.Name)).ToString();
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the data of a player
        /// </summary>
        /// <response code="200">User</response>
        /// <response code="404">The player was not found</response>
        /// <response code="403">The player is banned</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [HttpGet("get-player/{playerId:int}")]
        [ProducesResponseType(typeof(PlayerInfoByIdResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PlayerInfoByIdResponse>> GetPlayer([FromRoute] int playerId)
        {
            if (!HasAccessToPlayer(playerId))
            {
                return Unauthorized();
            }

            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _playerService.GetPlayerById(playerId, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Changes a user
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="request">Request model to change a player</param>
        /// <response code="200">The user was changed</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">The player was not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [HttpPost("edit-player/{playerId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EditPlayer([FromRoute] int playerId, [FromBody] EditPlayerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!HasAccessToPlayer(playerId))
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var result = await _playerService.EditPlayer(playerId, request, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the top 100 list of players
        /// </summary>
        /// <response code="200">List of users</response>
        /// <response code="404">No players were found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-top100-players")]
        [ProducesResponseType(typeof(PlayerInfoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PlayerInfoResponse>>> GetTop100Players()
        {
            var result = await _playerService.GetTop100Players();
            return ToActionResult(result);
        }

        #endregion Player Data Methods

        #region Statistics Methods

        /// <summary>
        /// Gets the statistics of all players
        /// </summary>
        /// <response code="200">Statistics of all players</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-all-players-statistics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PlayerStatisticsResponse>>> GetAllPlayersStatistics()
        {
            var result = await _playerStatisticsService.GetAllPlayersStatistics();
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the statistics of a player
        /// </summary>
        /// <response code="200">Statistics of the player</response>
        /// <response code="400">The player was not found</response>
        /// <response code="403">The player is banned</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-player-statistics/{playerId:int}")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PlayerStatisticsResponse>> GetPlayerStatistics([FromRoute] int playerId)
        {
            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _playerStatisticsService.GetPlayerStatistics(playerId, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the statistics of the rounds a player played for a withdrawal
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <response code="200">Statistics of played rounds for a withdrawal</response>
        /// <response code="404">The player or the statistics were not found</response>
        /// <response code="403">The player is banned</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [HttpGet("get-player-rounds-statistics-for-withdrawal/{playerId:int}")]
        [ProducesResponseType(typeof(PlayerRoundsStatisticsForWithdrawalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PlayerRoundsStatisticsForWithdrawalDTO>> GetPlayerRoundsStatisticsForWithdrawal([FromRoute] int playerId)
        {
            if (!HasAccessToPlayer(playerId))
            {
                return Unauthorized();
            }

            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _playerStatisticsService.GetPlayerRoundsStatisticsForWithdrawal(playerId, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the statistics of the rounds played for a withdrawal by all players
        /// </summary>
        /// <response code="200">List of the statistics of played rounds for a withdrawal</response>
        /// <response code="404">The statistics were not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrModerator)]
        [HttpGet("get-all-players-rounds-statistics-for-withdrawal")]
        [ProducesResponseType(typeof(IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>> GetAllPlayersRoundsStatisticsForWithdrawal()
        {
            var result = await _playerStatisticsService.GetAllPlayersRoundsStatisticsForWithdrawal();
            return ToActionResult(result);
        }

        #endregion Statistics Methods

        #region Private Methods

        /// <summary>
        /// Checks whether the current user has access to the player's data
        /// </summary>
        private bool HasAccessToPlayer(int playerId)
        {
            if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Moderator))
            {
                return true;
            }

            var currentPlayerId = GetCurrentPlayerId();
            return currentPlayerId == playerId;
        }

        /// <summary>
        /// Gets the id of the current player from the claims
        /// </summary>
        private int? GetCurrentPlayerId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (claim is not null && int.TryParse(claim.Value, out var playerId))
            {
                return playerId;
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the category attribute of a property
        /// </summary>
        public static string GetCategoryAttributeValue(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttribute<CategoryAttribute>();
            return attribute?.Category!;
        }

        #endregion Private Methods
    }
}
