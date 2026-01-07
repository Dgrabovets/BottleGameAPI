using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BottleGame.Infrastructure.Data;
using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BottleGame.Application.Interfaces;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис кэширования справочных данных
    /// </summary>
    public class CacheService : ICacheService
    {
        #region Constants

        /// <summary>
        /// Ключ кэша для игровых параметров
        /// </summary>
        private const string GameParamsCacheKey = "GameParams";

        /// <summary>
        /// Ключ кэша для типов транзакций
        /// </summary>
        private const string TransactionTypesCacheKey = "TransactionTypes";

        /// <summary>
        /// Ключ кэша для статусов транзакций
        /// </summary>
        private const string TransactionStatusesCacheKey = "TransactionStatuses";

        /// <summary>
        /// Время жизни кэша по умолчанию (5 минут)
        /// </summary>
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Время жизни кэша для справочников (30 минут)
        /// </summary>
        private static readonly TimeSpan ReferenceCacheDuration = TimeSpan.FromMinutes(30);

        #endregion Constants

        #region Fields

        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CacheService> _logger;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CacheService"/>
        /// </summary>
        /// <param name="cache">Кэш в памяти</param>
        /// <param name="scopeFactory">Фабрика скоупов для создания DbContext</param>
        /// <param name="logger">Логгер</param>
        public CacheService(
            IMemoryCache cache,
            IServiceScopeFactory scopeFactory,
            ILogger<CacheService> logger)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        #endregion Constructor

        #region Public Methods

        /// <inheritdoc/>
        public async Task<GameParams?> GetGameParamsAsync()
        {
            return await _cache.GetOrCreateAsync(GameParamsCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheDuration;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var gameParams = await context.GameParams
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                _logger.LogDebug("[CacheService] GameParams загружены в кэш");

                return gameParams;
            });
        }

        /// <inheritdoc/>
        public async Task<Dictionary<int, string>> GetTransactionTypesAsync()
        {
            return await _cache.GetOrCreateAsync(TransactionTypesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ReferenceCacheDuration;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var types = await context.TransactionTypes
                    .AsNoTracking()
                    .ToDictionaryAsync(t => t.Id, t => t.Name);

                _logger.LogDebug("[CacheService] TransactionTypes загружены в кэш ({Count} записей)", types.Count);

                return types;
            }) ?? new Dictionary<int, string>();
        }

        /// <inheritdoc/>
        public async Task<Dictionary<int, string>> GetTransactionStatusesAsync()
        {
            return await _cache.GetOrCreateAsync(TransactionStatusesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ReferenceCacheDuration;

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var statuses = await context.TransactionStatuses
                    .AsNoTracking()
                    .ToDictionaryAsync(s => s.Id, s => s.Name);

                _logger.LogDebug("[CacheService] TransactionStatuses загружены в кэш ({Count} записей)", statuses.Count);

                return statuses;
            }) ?? new Dictionary<int, string>();
        }

        /// <inheritdoc/>
        public void InvalidateGameParams()
        {
            _cache.Remove(GameParamsCacheKey);
            _logger.LogDebug("[CacheService] Кэш GameParams инвалидирован");
        }

        /// <inheritdoc/>
        public void InvalidateTransactionTypes()
        {
            _cache.Remove(TransactionTypesCacheKey);
            _logger.LogDebug("[CacheService] Кэш TransactionTypes инвалидирован");
        }

        /// <inheritdoc/>
        public void InvalidateTransactionStatuses()
        {
            _cache.Remove(TransactionStatusesCacheKey);
            _logger.LogDebug("[CacheService] Кэш TransactionStatuses инвалидирован");
        }

        #endregion Public Methods
    }
}




