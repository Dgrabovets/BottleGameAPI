using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// <inheritdoc cref="IDatabaseHealthService"/>
    /// </summary>
    public class DatabaseHealthService : IDatabaseHealthService
    {
        #region Fields

        /// <inheritdoc cref="ApplicationDbContext"/>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<DatabaseHealthService> _logger;

        /// <summary>
        /// Prefix of the log messages
        /// </summary>
        private const string PREFIX = "[Database Health]";

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="DatabaseHealthService"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger</param>
        public DatabaseHealthService(ApplicationDbContext context, ILogger<DatabaseHealthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Methods

        /// <summary>
        /// <inheritdoc cref="IDatabaseHealthService.CheckAsync"/>
        /// </summary>
        public async Task<object> CheckAsync()
        {
            try
            {
                _logger.LogDebug("{Prefix} Checking database connection", PREFIX);

                if (!await _context.Database.CanConnectAsync())
                {
                    _logger.LogWarning("{Prefix} Database connection failed", PREFIX);
                    return new { status = "error", message = "Cannot connect to database" };
                }

                var playerCount = await _context.Players.CountAsync();
                _logger.LogDebug("{Prefix} Database check passed. Players: {Count}", PREFIX, playerCount);

                return new
                {
                    status = "ok",
                    players = playerCount,
                    connected = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Database health check failed", PREFIX);
                return new { status = "error", message = "Database query failed" };
            }
        }

        #endregion Methods
    }
}
