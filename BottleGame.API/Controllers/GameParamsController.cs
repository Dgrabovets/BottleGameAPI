using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;
using BottleGame.Domain.Enums;
using BottleGame.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Game parameters controller
    /// </summary>
    /// <remarks>
    /// Manages the game settings, payment methods and currencies
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of <see cref="GameParamsController"/>
    /// </remarks>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger</param>
    /// <param name="cacheService">Cache service</param>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Game parameters")]
    public class GameParamsController(ApplicationDbContext context, ILogger<GameParamsController> logger, ICacheService cacheService) : Controller
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<GameParamsController> _logger = logger;

        /// <summary>
        /// Cache service
        /// </summary>
        private readonly ICacheService _cacheService = cacheService;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[GameParams Controller]";

        #endregion Fields

        #region ActionMethods

        /// <summary>
        /// Gets the game parameters
        /// </summary>
        /// <response code="200">Game parameters</response>
        /// <response code="404">The parameters were not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = "Admin,Player")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-game-params")]
        public async Task<ActionResult<GameParams>> GetGameParams()
        {
            try
            {
                // Get the parameters from the cache
                var gameParams = await _cacheService.GetGameParamsAsync();

                if (gameParams is null)
                {
                    _logger.LogWarning("{Prefix} Game parameters not found", Prefix);
                    return NotFound($"Game parameters not found");
                }

                _logger.LogInformation("{Prefix} Game parameters found (from the cache)", Prefix);

                return Ok(gameParams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error while getting the game parameters", Prefix);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Updates the game parameters
        /// </summary>
        /// <param name="inputGameParams">Game parameters</param>
        /// <response code="200">The parameters were changed</response>
        /// <response code="404">The parameters were not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("update-game-params")]
        public async Task<ActionResult> UpdateGameParams([FromBody] GameParams inputGameParams)
        {
            try
            {
                // Check the input data
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Invalid data for changing the game parameters", Prefix);
                    return BadRequest(ModelState);
                }

                // Find the parameters
                var gameParams = await _context.GameParams.FirstOrDefaultAsync();

                if (gameParams is null)
                {
                    _logger.LogWarning("{Prefix} Game parameters not found", Prefix);
                    return NotFound($"Game parameters not found");
                }

                _context.Entry(gameParams).CurrentValues.SetValues(inputGameParams);

                await _context.SaveChangesAsync();

                // Invalidate the cache after the update
                _cacheService.InvalidateGameParams();

                _logger.LogInformation("{Prefix} Game parameters updated, cache invalidated", Prefix);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error while updating the game parameters", Prefix);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets all available payout methods
        /// </summary>
        /// <response code="200">List of all payout methods</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = "Player,Admin")]
        [HttpGet("get-all-payout-methods")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<string>> GetAllPayoutMethods()
        {
            try
            {
                var methods = Enum.GetNames<PayoutMethodType>();
                return Ok(methods);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets all available deposit methods
        /// </summary>
        /// <response code="200">List of all deposit methods</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = "Player,Admin")]
        [HttpGet("get-all-payment-methods")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<string>> GetAllPaymentMethods()
        {
            try
            {
                var methods = Enum.GetNames<PaymentMethodType>();
                return Ok(methods);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets all available currencies
        /// </summary>
        /// <response code="200">List of all currencies</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = "Player,Admin")]
        [HttpGet("get-all-currencies")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<string>> GetAllCurrencies()
        {
            try
            {
                var currencies = Enum.GetNames<CurrencyType>();
                return Ok(currencies);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion ActionMethods
    }
}
