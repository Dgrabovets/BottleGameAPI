using BottleGame.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер проверки состояния сервисов
    /// </summary>
    /// <remarks>
    /// Предоставляет эндпоинт /api/health для мониторинга состояния API, 
    /// подключения к базе данных и ключевых сервисов. Не отображается в Swagger.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HealthController(
        ApplicationDbContext context,
        ILogger<HealthController> logger) : ControllerBase
    {
        #region Fields

        /// <inheritdoc cref="ApplicationDbContext"/>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<HealthController> _logger = logger;

        private const string Prefix = "[Health Controller]";

        #endregion Fields

        #region Public Methods

        /// <summary>
        /// Проверка общего состояния API и зависимостей
        /// </summary>
        /// <returns>JSON объект с результатами проверок</returns>
        /// <response code="200">Все сервисы работают корректно</response>
        /// <response code="500">Внутренняя ошибка при выполнении проверок</response>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Check()
        {
            try
            {
                _logger.LogInformation("{Prefix} Health check initiated", Prefix);

                var checks = new Dictionary<string, object>
                {
                    ["status"] = "healthy",
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["version"] = "1.0.0",
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
        /// Проверка состояния базы данных
        /// </summary>
        /// <returns>Результат проверки подключения и статистики</returns>
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
