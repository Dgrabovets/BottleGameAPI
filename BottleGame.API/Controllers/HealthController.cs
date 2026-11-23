using BottleGame.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class HealthController(ApplicationDbContext context, ILogger<HealthController> logger) : ControllerBase
    {
        #region Fields

        /// <inheritdoc cref="ApplicationDbContext"/>
        private readonly ApplicationDbContext _context = context;

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

                var version = GetApplicationVersion();
                var checks = new Dictionary<string, object>
                {
                    ["status"] = "healthy",
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["version"] = version,
                    ["database"] = await CheckDatabase(),
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

        #region Private Methods

        /// <summary>
        /// Version from <c>BottleGame.API.csproj</c> → <c>Version</c>
        /// </summary>
        private static string GetApplicationVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "0.0.0";
        }

        /// <summary>
        /// Checks the state of the database
        /// </summary>
        /// <returns>Result of the connection check and statistics</returns>
        private async Task<object> CheckDatabase()
        {
            try
            {
                _logger.LogDebug("{Prefix} Checking database connection", Prefix);

                var canConnect = _context.Database.CanConnect();
                if (!canConnect)
                {
                    _logger.LogWarning("{Prefix} Database connection failed", Prefix);
                    return new { status = "error", message = "Cannot connect to database" };
                }

                var playerCount = await _context.Players.CountAsync();
                _logger.LogDebug("{Prefix} Database check passed. Players: {Count}", Prefix, playerCount);

                return new
                {
                    status = "ok",
                    players = playerCount,
                    connected = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Database health check failed", Prefix);
                return new { status = "error", message = "Database query failed" };
            }
        }

        #endregion Private Methods
    }
}
