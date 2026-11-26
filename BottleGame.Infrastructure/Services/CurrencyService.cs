using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.External.Processing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Service for working with currencies and exchange rates.
    /// Uses caching to optimize requests to the payment provider
    /// </summary>
    public class CurrencyService : ICurrencyService
    {
        #region Fields

        /// <summary>
        /// Payment provider for getting the catalogs
        /// </summary>
        private readonly IPaymentProvider _paymentProvider;

        /// <summary>
        /// Cache for storing the catalogs
        /// </summary>
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Logger for recording events
        /// </summary>
        private readonly ILogger<CurrencyService> _logger;

        /// <summary>
        /// Whether the service works with the real processing
        /// </summary>
        private readonly bool _isLive;

        /// <summary>
        /// Cache key for the list of currencies
        /// </summary>
        private const string CURRENCIES_CACHE_KEY = "currencies";

        /// <summary>
        /// Cache key for the list of payment gateways
        /// </summary>
        private const string GATEWAYS_CACHE_KEY = "payment_gateways";

        /// <summary>
        /// Prefix of the cache key for exchange rates
        /// </summary>
        private const string EXCHANGE_RATE_CACHE_KEY_PREFIX = "exchange_rate_";

        /// <summary>
        /// Lifetime of the catalog cache (5 minutes)
        /// </summary>
        private static readonly TimeSpan CatalogCacheExpiration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Lifetime of the rate cache (1 minute)
        /// </summary>
        private static readonly TimeSpan ExchangeRateCacheExpiration = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string PREFIX = "[CurrencyService]";

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="CurrencyService"/>
        /// </summary>
        /// <param name="paymentProvider">Payment provider</param>
        /// <param name="cache">Cache</param>
        /// <param name="logger">Logger</param>
        /// <param name="options">Processing settings</param>
        public CurrencyService(IPaymentProvider paymentProvider,
                               IMemoryCache cache,
                               ILogger<CurrencyService> logger,
                               IOptions<ProcessingOptions> options)
        {
            _paymentProvider = paymentProvider;
            _cache = cache;
            _logger = logger;
            _isLive = options.Value.Mode == ProcessingMode.Live;
        }

        #region Public Methods

        /// <inheritdoc/>
        public async Task<IEnumerable<CurrencyInfo>> GetAvailableCurrenciesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (!_isLive)
            {
                return GetSimulatedCurrencies();
            }

            if (forceRefresh)
            {
                _cache.Remove(CURRENCIES_CACHE_KEY);
            }

            var currencies = await _cache.GetOrCreateAsync(CURRENCIES_CACHE_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CatalogCacheExpiration;

                _logger.LogInformation("{Prefix} Loading the list of currencies from the API", PREFIX);
                var result = await _paymentProvider.GetCurrenciesAsync(cancellationToken);
                return result.ToList();
            });

            return currencies ?? Enumerable.Empty<CurrencyInfo>();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (!_isLive)
            {
                return GetSimulatedPaymentGateways();
            }

            if (forceRefresh)
            {
                _cache.Remove(GATEWAYS_CACHE_KEY);
            }

            var gateways = await _cache.GetOrCreateAsync(GATEWAYS_CACHE_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CatalogCacheExpiration;

                _logger.LogInformation("{Prefix} Loading the list of payment gateways from the API", PREFIX);
                var result = await _paymentProvider.GetPaymentGatewaysAsync(cancellationToken);
                return result.ToList();
            });

            return gateways ?? Enumerable.Empty<PaymentGatewayInfo>();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysForCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default)
        {
            var gateways = await GetPaymentGatewaysAsync(cancellationToken: cancellationToken);

            return gateways.Where(g =>
                g.SupportsPayments &&
                g.Currency.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PaymentGatewayInfo>> GetPayoutGatewaysForCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default)
        {
            var gateways = await GetPaymentGatewaysAsync(cancellationToken: cancellationToken);

            return gateways.Where(g =>
                g.SupportsPayouts &&
                g.Currency.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public async Task<decimal> GetExchangeRateAsync(string currencyCode, CancellationToken cancellationToken = default)
        {
            if (!_isLive)
            {
                return SIMULATED_EXCHANGE_RATE;
            }

            var cacheKey = $"{EXCHANGE_RATE_CACHE_KEY_PREFIX}{currencyCode.ToLowerInvariant()}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ExchangeRateCacheExpiration;

                _logger.LogDebug("{Prefix} Loading the rate for {Currency} from the API", PREFIX, LogSanitizer.Clean(currencyCode));
                return await _paymentProvider.GetExchangeRateAsync(currencyCode, cancellationToken);
            });
        }

        /// <inheritdoc/>
        public async Task<decimal> ConvertToBaseCurrencyAsync(decimal amount, string fromCurrencyCode, CancellationToken cancellationToken = default)
        {
            var rate = await GetExchangeRateAsync(fromCurrencyCode, cancellationToken);

            if (rate <= 0)
            {
                _logger.LogWarning("{Prefix} Rate for {Currency} is unavailable, returning the original amount", PREFIX, fromCurrencyCode);
                return amount;
            }

            var converted = amount / rate;
            _logger.LogDebug("{Prefix} Conversion {Amount} {Currency} -> {Converted} (rate: {Rate})",
                PREFIX, amount, fromCurrencyCode, converted, rate);

            return converted;
        }

        /// <inheritdoc/>
        public async Task<decimal> ConvertFromBaseCurrencyAsync(decimal amount, string toCurrencyCode, CancellationToken cancellationToken = default)
        {
            var rate = await GetExchangeRateAsync(toCurrencyCode, cancellationToken);

            if (rate <= 0)
            {
                _logger.LogWarning("{Prefix} Rate for {Currency} is unavailable, returning the original amount", PREFIX, toCurrencyCode);
                return amount;
            }

            var converted = amount * rate;
            _logger.LogDebug("{Prefix} Conversion {Amount} -> {Converted} {Currency} (rate: {Rate})",
                PREFIX, amount, converted, toCurrencyCode, rate);

            return converted;
        }

        /// <inheritdoc/>
        public async Task<bool> IsCurrencyActiveAsync(string currencyCode, CancellationToken cancellationToken = default)
        {
            var currencies = await GetAvailableCurrenciesAsync(cancellationToken: cancellationToken);

            return currencies.Any(c =>
                c.Code.Equals(currencyCode, StringComparison.OrdinalIgnoreCase) &&
                c.IsActive);
        }

        /// <inheritdoc/>
        public async Task<bool> IsAmountValidForGatewayAsync(string gatewayCode, decimal amount, CancellationToken cancellationToken = default)
        {
            var gateways = await GetPaymentGatewaysAsync(cancellationToken: cancellationToken);
            var gateway = gateways.FirstOrDefault(g => g.Code.Equals(gatewayCode, StringComparison.OrdinalIgnoreCase));

            if (gateway is null)
            {
                _logger.LogWarning("{Prefix} Gateway {Gateway} not found", PREFIX, LogSanitizer.Clean(gatewayCode));
                return false;
            }

            var isValid = true;

            if (gateway.MinAmount.HasValue && amount < gateway.MinAmount.Value)
            {
                isValid = false;
            }

            if (gateway.MaxAmount.HasValue && amount > gateway.MaxAmount.Value)
            {
                isValid = false;
            }

            if (!isValid)
            {
                _logger.LogWarning("{Prefix} Amount {Amount} is outside the allowed range for gateway {Gateway} (min: {Min}, max: {Max})",
                    PREFIX, amount, LogSanitizer.Clean(gatewayCode), gateway.MinAmount, gateway.MaxAmount);
            }

            return isValid;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetDetailTypesForGatewayAsync(string gatewayCode, CancellationToken cancellationToken = default)
        {
            var gateways = await GetPaymentGatewaysAsync(cancellationToken: cancellationToken);
            var gateway = gateways.FirstOrDefault(g => g.Code.Equals(gatewayCode, StringComparison.OrdinalIgnoreCase));

            if (gateway is null)
            {
                _logger.LogWarning("{Prefix} Gateway {Gateway} not found", PREFIX, LogSanitizer.Clean(gatewayCode));
                return [];
            }

            return gateway.DetailTypes;
        }

        /// <inheritdoc/>
        public async Task<bool> IsDetailTypeValidForGatewayAsync(string gatewayCode, string detailType, CancellationToken cancellationToken = default)
        {
            var detailTypes = await GetDetailTypesForGatewayAsync(gatewayCode, cancellationToken);

            if (!detailTypes.Any())
            {
                // If the gateway has no restrictions on detail types, any type is considered valid
                _logger.LogDebug("{Prefix} Gateway {Gateway} has no restrictions on detail types", PREFIX, LogSanitizer.Clean(gatewayCode));
                return true;
            }

            var isValid = detailTypes.Any(dt => dt.Equals(detailType, StringComparison.OrdinalIgnoreCase));

            if (!isValid)
            {
                _logger.LogWarning("{Prefix} Detail type '{DetailType}' is not supported by gateway {Gateway}. Available: {Available}",
                    PREFIX, LogSanitizer.Clean(detailType), LogSanitizer.Clean(gatewayCode), string.Join(", ", detailTypes));
            }

            return isValid;
        }

        /// <inheritdoc/>
        public void ClearCache()
        {
            _cache.Remove(CURRENCIES_CACHE_KEY);
            _cache.Remove(GATEWAYS_CACHE_KEY);
            _logger.LogInformation("{Prefix} Catalog cache cleared", PREFIX);
        }

        #endregion Public Methods

        #region Simulated Data

        /// <summary>
        /// Currency code in Simulated mode (matches the player registration)
        /// </summary>
        private const string SIMULATED_CURRENCY_CODE = "rub";

        /// <summary>
        /// Gateway in Simulated mode (matches PaymentMethod/PayoutMethod at registration)
        /// </summary>
        private const string SIMULATED_GATEWAY_CODE = "sberbank";

        /// <summary>
        /// Rate in Simulated mode: no conversion
        /// </summary>
        private const decimal SIMULATED_EXCHANGE_RATE = 1m;

        /// <summary>
        /// Returns a single currency without calling the processing
        /// </summary>
        private static IReadOnlyList<CurrencyInfo> GetSimulatedCurrencies() =>
        [
            new CurrencyInfo(SIMULATED_CURRENCY_CODE, "Russian ruble (test)", isActive: true)
        ];

        /// <summary>
        /// Returns a single gateway for deposits and withdrawals without calling the processing
        /// </summary>
        private static IReadOnlyList<PaymentGatewayInfo> GetSimulatedPaymentGateways() =>
        [
            new PaymentGatewayInfo(
                code: SIMULATED_GATEWAY_CODE,
                name: "Sberbank (test)",
                currency: SIMULATED_CURRENCY_CODE,
                supportsPayments: true,
                supportsPayouts: true,
                minAmount: 100m,
                maxAmount: 1_000_000m,
                detailTypes: ["card", "phone", "sbp"])
        ];

        #endregion Simulated Data
    }
}
