using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Reference data caching service
    /// </summary>
    public class CacheService : ICacheService
    {
        #region Constants

        /// <summary>
        /// Cache key for the game parameters
        /// </summary>
        private const string GAME_PARAMS_CACHE_KEY = "GameParams";

        /// <summary>
        /// Cache key for the transaction types
        /// </summary>
        private const string TRANSACTION_TYPES_CACHE_KEY = "TransactionTypes";

        /// <summary>
        /// Cache key for the transaction statuses
        /// </summary>
        private const string TRANSACTION_STATUSES_CACHE_KEY = "TransactionStatuses";

        /// <summary>
        /// Default cache lifetime (5 minutes)
        /// </summary>
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Cache lifetime for reference data (30 minutes)
        /// </summary>
        private static readonly TimeSpan ReferenceCacheDuration = TimeSpan.FromMinutes(30);

        #endregion Constants

        #region Fields

        /// <summary>
        /// In-memory cache
        /// </summary>
        private readonly IMemoryCache _cache;
        /// <summary>
        /// Scope factory for creating a DbContext
        /// </summary>
        private readonly IServiceScopeFactory _scopeFactory;
        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<CacheService> _logger;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="CacheService"/>
        /// </summary>
        /// <param name="cache">In-memory cache</param>
        /// <param name="scopeFactory">Scope factory for creating DbContext</param>
        /// <param name="logger">Logger</param>
        public CacheService(IMemoryCache cache, IServiceScopeFactory scopeFactory, ILogger<CacheService> logger)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        #region Public Methods

        /// <inheritdoc/>
        public async Task<GameParams?> GetGameParamsAsync()
        {
            return await _cache.GetOrCreateAsync(GAME_PARAMS_CACHE_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheDuration;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var gameParams = await context.GameParams
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                _logger.LogDebug("GameParams loaded into the cache");

                return gameParams;
            });
        }

        /// <inheritdoc/>
        public async Task<Dictionary<int, string>> GetTransactionTypesAsync()
        {
            return await _cache.GetOrCreateAsync(TRANSACTION_TYPES_CACHE_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ReferenceCacheDuration;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var types = await context.TransactionTypes
                    .AsNoTracking()
                    .ToDictionaryAsync(t => t.Id, t => t.Name);

                _logger.LogDebug("TransactionTypes loaded into the cache ({Count} records)", types.Count);

                return types;
            }) ?? [];
        }

        /// <inheritdoc/>
        public async Task<Dictionary<int, string>> GetTransactionStatusesAsync()
        {
            return await _cache.GetOrCreateAsync(TRANSACTION_STATUSES_CACHE_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ReferenceCacheDuration;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var statuses = await context.TransactionStatuses
                    .AsNoTracking()
                    .ToDictionaryAsync(s => s.Id, s => s.Name);

                _logger.LogDebug("TransactionStatuses loaded into the cache ({Count} records)", statuses.Count);

                return statuses;
            }) ?? [];
        }

        /// <inheritdoc/>
        public void InvalidateGameParams()
        {
            _cache.Remove(GAME_PARAMS_CACHE_KEY);
            _logger.LogDebug("GameParams cache invalidated");
        }

        /// <inheritdoc/>
        public void InvalidateTransactionTypes()
        {
            _cache.Remove(TRANSACTION_TYPES_CACHE_KEY);
            _logger.LogDebug("TransactionTypes cache invalidated");
        }

        /// <inheritdoc/>
        public void InvalidateTransactionStatuses()
        {
            _cache.Remove(TRANSACTION_STATUSES_CACHE_KEY);
            _logger.LogDebug("TransactionStatuses cache invalidated");
        }

        #endregion Public Methods
    }
}
