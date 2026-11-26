using BottleGame.API.Extensions;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Administration controller
    /// </summary>
    /// <remarks>
    /// Manages the application balance, statistics and moderators
    /// </remarks>
    [ApiController]
    [Route("api/admin")]
    [Tags("Administration")]
    public class AdministrationController : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Application balance service
        /// </summary>
        private readonly IAppBalanceService _appBalanceService;

        /// <summary>
        /// Statistics service
        /// </summary>
        private readonly IStatisticsService _statisticsService;

        /// <summary>
        /// Moderator management service
        /// </summary>
        private readonly IModeratorService _moderatorService;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="AdministrationController"/>
        /// </summary>
        /// <param name="appBalanceService">Application balance service</param>
        /// <param name="statisticsService">Statistics service</param>
        /// <param name="moderatorService">Moderator management service</param>
        public AdministrationController(IAppBalanceService appBalanceService,
                                        IStatisticsService statisticsService,
                                        IModeratorService moderatorService)
        {
            _appBalanceService = appBalanceService;
            _statisticsService = statisticsService;
            _moderatorService = moderatorService;
        }

        #region App Balance Methods

        /// <summary>
        /// Gets the current application balance
        /// </summary>
        [Authorize(Roles = RoleNames.ADMIN)]
        [HttpGet("get-app-balance")]
        [ProducesResponseType(typeof(AppBalanceDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AppBalanceDTO>> GetAppBalance()
        {
            var result = await _appBalanceService.GetAppBalance();
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates the lower threshold of the application balance
        /// </summary>
        /// <response code="200">The lower threshold was updated</response>
        /// <response code="400">No data to update was found, most likely the data integrity is broken or the request data is invalid</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.ADMIN)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("update-threshold")]
        public async Task<IActionResult> UpdateAppBalanceThreshold(
            [Description("Request to update the lower threshold of the application balance")]
            [FromBody] UpdateAppBalanceThresholdRequest request
        )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminId = User.GetUserId();
            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _appBalanceService.UpdateAppBalanceThreshold(request, adminId.Value);

            if (result.IsSuccess)
            {
                return Ok(new { Message = "The lower threshold was updated" });
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Tops up the application balance
        /// </summary>
        /// <response code="200">The balance was topped up</response>
        /// <response code="400">No data to update was found or the request data is invalid</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.ADMIN)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("topup-balance")]
        public async Task<IActionResult> TopUpAppBalance(
            [Description("Request to top up the application balance")]
            [FromBody] TopUpAppBalanceRequest request
        )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminId = User.GetUserId();
            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _appBalanceService.TopUpAppBalance(request, adminId.Value);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    Message = "The balance was topped up",
                    NewBalance = result.Value
                });
            }

            return ToActionResultObject(result);
        }

        #endregion App Balance Methods

        #region Statistics Methods

        /// <summary>
        /// Gets the administration statistics
        /// </summary>
        /// <response code="200">Statistics</response>
        /// <response code="400">The required data was not found, most likely the data integrity is broken</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.ADMIN)]
        [ProducesResponseType(typeof(GetAppGeneralStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-statistics")]
        public async Task<ActionResult<GetAppGeneralStatisticsResponse>> GetStatistics([FromQuery] DateTime? dateFrom,
                                                                                       [FromQuery] DateTime? dateTill)
        {
            var request = new GetAppGeneralStatisticsRequest
            {
                DateFrom = dateFrom,
                DateTill = dateTill
            };

            var result = await _statisticsService.GetStatistics(request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the statistics timeline for charts
        /// </summary>
        [Authorize(Roles = RoleNames.ADMIN)]
        [ProducesResponseType(typeof(GetAppStatisticsTimelineResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-statistics-timeline")]
        public async Task<ActionResult<GetAppStatisticsTimelineResponse>> GetStatisticsTimeline(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTill)
        {
            var request = new GetAppStatisticsTimelineRequest
            {
                DateFrom = dateFrom,
                DateTill = dateTill
            };

            var result = await _statisticsService.GetStatisticsTimeline(request);
            return ToActionResult(result);
        }

        #endregion Statistics Methods

        #region Moderator Methods

        /// <summary>
        /// Registers a new moderator
        /// </summary>
        /// <param name="authRequest">Registration data (login and password)</param>
        /// <response code="200">The moderator was registered</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="409">The moderator already exists</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.ADMIN)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("moderator-register")]
        public async Task<IActionResult> RegisterModerator(
            [Description("Credentials model for administration roles")]
            [FromBody] AdministrationRoleUserCredentials authRequest
        )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _moderatorService.RegisterModerator(authRequest);

            if (result.IsSuccess)
            {
                return Ok(new { Message = "The moderator was registered" });
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Gets the list of moderators or a single moderator by id
        /// </summary>
        /// <param name="moderatorId">Moderator id (optional)</param>
        /// <response code="200">Successful request</response>
        /// <response code="404">The moderator was not found (when requested by id)</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.ADMIN)]
        [ProducesResponseType(typeof(IEnumerable<ModeratorDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModeratorDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-moderators")]
        public async Task<IActionResult> GetModerators(
            [Description("Moderator identifier")]
            [FromQuery] int? moderatorId = null
        )
        {
            var result = await _moderatorService.GetModerators(moderatorId);
            return ToActionResultObject(result);
        }

        /// <summary>
        /// Deletes a moderator by id
        /// </summary>
        /// <param name="moderatorId">Moderator id</param>
        /// <response code="200">The moderator was deleted</response>
        /// <response code="404">The moderator was not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.ADMIN)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete("delete-moderator/{moderatorId}")]
        public async Task<IActionResult> DeleteModerator(
            [Description("Moderator identifier")]
            [FromRoute] int moderatorId
        )
        {
            var result = await _moderatorService.DeleteModerator(moderatorId);

            if (result.IsSuccess)
            {
                return Ok(new { Message = "The moderator was deleted" });
            }

            return ToActionResult(result);
        }

        #endregion Moderator Methods
    }
}
