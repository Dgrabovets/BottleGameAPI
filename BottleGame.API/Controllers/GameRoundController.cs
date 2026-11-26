using BottleGame.API.Extensions;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Game round controller
    /// </summary>
    /// <remarks>
    /// Manages game rounds, finishing games and getting statistics
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Game rounds")]
    public class GameRoundController : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Service for working with game rounds
        /// </summary>
        private readonly IGameRoundService _gameRoundService;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="GameRoundController"/>
        /// </summary>
        /// <param name="gameRoundService">Service for working with game rounds</param>
        public GameRoundController(IGameRoundService gameRoundService)
        {
            _gameRoundService = gameRoundService;
        }

        #region Action Methods

        /// <summary>
        /// Finishes a round
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="request">Request to finish a round</param>
        /// <response code="200">The round was finished</response>
        /// <response code="400">Invalid request</response>
        /// <response code="403">The player is banned</response>
        /// <response code="404">The player was not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("game")]
        [HttpPost("finish-game-round/{playerId:int}")]
        public async Task<IActionResult> FinishGameRound([FromRoute] int playerId, [FromBody] FinishRoundRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var requestingPlayerId = User.GetUserId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _gameRoundService.FinishGameRound(playerId, request, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the rounds played by a player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <response code="200">List of rounds</response>
        /// <response code="404">The player was not found</response>
        /// <response code="403">The player is banned</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(typeof(GetPlayerPlayedRoundsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-player-played-rounds/{playerId:int}")]
        public async Task<ActionResult<GetPlayerPlayedRoundsResponse>> GetPlayerPlayedRounds([FromRoute] int playerId)
        {
            var result = await _gameRoundService.GetPlayerPlayedRounds(playerId);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the profit for a period
        /// </summary>
        /// <response code="200">Profit</response>
        /// <response code="404">No rounds were found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(typeof(GetProfitResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-profit")]
        public async Task<ActionResult<GetProfitResponse>> GetProfit([FromBody] GetProfitRequest request)
        {
            var result = await _gameRoundService.GetProfit(request);
            return ToActionResult(result);
        }

        #endregion Action Methods
    }
}
