using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.External.Processing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис для работы с валютами и курсами обмена.
    /// Использует кэширование для оптимизации запросов к платёжному провайдеру.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="CurrencyService"/>.
    /// </remarks>
    /// <param name="paymentProvider">Платёжный провайдер.</param>
    /// <param name="cache">Кэш.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="options">Настройки процессинга.</param>
    public class CurrencyService(
        IPaymentProvider paymentProvider,
        IMemoryCache cache,
        ILogger<CurrencyService> logger,
        IOptions<ProcessingOptions> options) : ICurrencyService
    {
        #region Fields

        /// <summary>
        /// Платёжный провайдер для получения справочников.
        /// </summary>
        private readonly IPaymentProvider _paymentProvider = paymentProvider;

        /// <summary>
        /// Кэш для хранения справочников.
        /// </summary>
        private readonly IMemoryCache _cache = cache;

        /// <summary>
        /// Логгер для записи событий.
        /// </summary>
        private readonly ILogger<CurrencyService> _logger = logger;

        /// <summary>
        /// Работает ли сервис с реальным процессингом.
        /// </summary>
        private readonly bool _isLive = options.Value.Mode == ProcessingMode.Live;

        /// <summary>
        /// Ключ кэша для списка валют.
        /// </summary>
        private const string CurrenciesCacheKey = "currencies";

        /// <summary>
        /// Ключ кэша для списка платёжных шлюзов.
        /// </summary>
        private const string GatewaysCacheKey = "payment_gateways";

        /// <summary>
        /// Префикс ключа кэша для курсов обмена.
        /// </summary>
        private const string ExchangeRateCacheKeyPrefix = "exchange_rate_";

        /// <summary>
        /// Время жизни кэша справочников (5 минут).
        /// </summary>
        private static readonly TimeSpan CatalogCacheExpiration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Время жизни кэша курсов (1 минута).
        /// </summary>
        private static readonly TimeSpan ExchangeRateCacheExpiration = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Префикс для логов.
        /// </summary>
        private const string Prefix = "[CurrencyService]";

        #endregion Fields

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
                _cache.Remove(CurrenciesCacheKey);
            }

            var currencies = await _cache.GetOrCreateAsync(CurrenciesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CatalogCacheExpiration;

                _logger.LogInformation("{Prefix} Загрузка списка валют из API", Prefix);
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
                _cache.Remove(GatewaysCacheKey);
            }

            var gateways = await _cache.GetOrCreateAsync(GatewaysCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CatalogCacheExpiration;

                _logger.LogInformation("{Prefix} Загрузка списка платёжных шлюзов из API", Prefix);
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
                return SimulatedExchangeRate;
            }

            var cacheKey = $"{ExchangeRateCacheKeyPrefix}{currencyCode.ToLowerInvariant()}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ExchangeRateCacheExpiration;

                _logger.LogDebug("{Prefix} Загрузка курса для {Currency} из API", Prefix, currencyCode);
                return await _paymentProvider.GetExchangeRateAsync(currencyCode, cancellationToken);
            });
        }

        /// <inheritdoc/>
        public async Task<decimal> ConvertToBaseCurrencyAsync(decimal amount, string fromCurrencyCode, CancellationToken cancellationToken = default)
        {
            var rate = await GetExchangeRateAsync(fromCurrencyCode, cancellationToken);

            if (rate <= 0)
            {
                _logger.LogWarning("{Prefix} Курс для {Currency} недоступен, возвращаем исходную сумму", Prefix, fromCurrencyCode);
                return amount;
            }

            var converted = amount / rate;
            _logger.LogDebug("{Prefix} Конвертация {Amount} {Currency} -> {Converted} (курс: {Rate})",
                Prefix, amount, fromCurrencyCode, converted, rate);

            return converted;
        }

        /// <inheritdoc/>
        public async Task<decimal> ConvertFromBaseCurrencyAsync(decimal amount, string toCurrencyCode, CancellationToken cancellationToken = default)
        {
            var rate = await GetExchangeRateAsync(toCurrencyCode, cancellationToken);

            if (rate <= 0)
            {
                _logger.LogWarning("{Prefix} Курс для {Currency} недоступен, возвращаем исходную сумму", Prefix, toCurrencyCode);
                return amount;
            }

            var converted = amount * rate;
            _logger.LogDebug("{Prefix} Конвертация {Amount} -> {Converted} {Currency} (курс: {Rate})",
                Prefix, amount, converted, toCurrencyCode, rate);

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
                _logger.LogWarning("{Prefix} Шлюз {Gateway} не найден", Prefix, gatewayCode);
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
                _logger.LogWarning("{Prefix} Сумма {Amount} вне допустимого диапазона для шлюза {Gateway} (мин: {Min}, макс: {Max})",
                    Prefix, amount, gatewayCode, gateway.MinAmount, gateway.MaxAmount);
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
                _logger.LogWarning("{Prefix} Шлюз {Gateway} не найден", Prefix, gatewayCode);
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
                // Если шлюз не имеет ограничений по типам реквизитов, считаем любой тип допустимым
                _logger.LogDebug("{Prefix} Шлюз {Gateway} не имеет ограничений по типам реквизитов", Prefix, gatewayCode);
                return true;
            }

            var isValid = detailTypes.Any(dt => dt.Equals(detailType, StringComparison.OrdinalIgnoreCase));

            if (!isValid)
            {
                _logger.LogWarning("{Prefix} Тип реквизитов '{DetailType}' не поддерживается шлюзом {Gateway}. Доступные: {Available}",
                    Prefix, detailType, gatewayCode, string.Join(", ", detailTypes));
            }

            return isValid;
        }

        /// <inheritdoc/>
        public void ClearCache()
        {
            _cache.Remove(CurrenciesCacheKey);
            _cache.Remove(GatewaysCacheKey);
            _logger.LogInformation("{Prefix} Кэш справочников очищен", Prefix);
        }

        #endregion Public Methods

        #region Simulated Data

        /// <summary>
        /// Код валюты в режиме Simulated (совпадает с регистрацией игрока)
        /// </summary>
        private const string SimulatedCurrencyCode = "rub";

        /// <summary>
        /// Шлюз в режиме Simulated (совпадает с PaymentMethod/PayoutMethod при регистрации)
        /// </summary>
        private const string SimulatedGatewayCode = "sberbank";

        /// <summary>
        /// Курс в режиме Simulated: конвертации нет
        /// </summary>
        private const decimal SimulatedExchangeRate = 1m;

        /// <summary>
        /// Возвращает одну валюту без обращения к процессингу
        /// </summary>
        private static IReadOnlyList<CurrencyInfo> GetSimulatedCurrencies() =>
        [
            new CurrencyInfo(SimulatedCurrencyCode, "Российский рубль (тест)", isActive: true)
        ];

        /// <summary>
        /// Возвращает один шлюз для пополнений и выводов без обращения к процессингу
        /// </summary>
        private static IReadOnlyList<PaymentGatewayInfo> GetSimulatedPaymentGateways() =>
        [
            new PaymentGatewayInfo(
                code: SimulatedGatewayCode,
                name: "Sberbank (тест)",
                currency: SimulatedCurrencyCode,
                supportsPayments: true,
                supportsPayouts: true,
                minAmount: 100m,
                maxAmount: 1_000_000m,
                detailTypes: ["card", "phone", "sbp"])
        ];

        #endregion Simulated Data
    }
}

