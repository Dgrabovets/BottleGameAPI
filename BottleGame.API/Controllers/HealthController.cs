using BottleGame.API.Extensions;
using BottleGame.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Service health controller
    /// </summary>
    /// <remarks>
    /// Provides the /api/health endpoint for monitoring the state of the API,
    /// the database connection and key services. It is not shown in the API reference
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HealthController(IDatabaseHealthService databaseHealthService, ILogger<HealthController> logger) : ControllerBase
    {
        #region Fields

        /// <inheritdoc cref="IDatabaseHealthService"/>
        private readonly IDatabaseHealthService _databaseHealthService = databaseHealthService;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<HealthController> _logger = logger;

        /// <summary>
        /// Prefix of the log messages
        /// </summary>
        private const string Prefix = "[Health Controller]";

        #endregion Fields

        #region Public Methods

        /// <summary>
        /// Checks the overall state of the API and its dependencies
        /// </summary>
        /// <returns>JSON object with the results of the checks</returns>
        /// <response code="200">All services work correctly</response>
        /// <response code="500">Internal error while running the checks</response>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Check()
        {
            try
            {
                _logger.LogInformation("{Prefix} Health check initiated", Prefix);

                var version = Assembly.GetExecutingAssembly().GetVersionText();
                var checks = new Dictionary<string, object>
                {
                    ["status"] = "healthy",
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["version"] = version,
                    ["database"] = await _databaseHealthService.CheckAsync(),
                    ["uptime"] = Environment.TickCount64 / 1000
                };

                _logger.LogInformation("{Prefix} Health check completed successfully", Prefix);
                return Ok(checks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Health check failed", Prefix);
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    error = "Health check execution failed",
                    timestamp = DateTime.UtcNow.ToString("O")
                });
            }
        }

        #endregion Public Methods
    }
}
