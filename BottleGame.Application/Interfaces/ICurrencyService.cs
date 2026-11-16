using BottleGame.Application.DTOs.Processing;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Service for working with currencies and exchange rates.
    /// Provides cached catalogs of currencies and payment gateways
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>
        /// Gets the list of available currencies (cached)
        /// </summary>
        /// <param name="forceRefresh">Forces a cache refresh</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of currency information</returns>
        Task<IEnumerable<CurrencyInfo>> GetAvailableCurrenciesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of available payment gateways (cached)
        /// </summary>
        /// <param name="forceRefresh">Forces a cache refresh</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of payment gateway information</returns>
        Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the payment gateways that accept payments for the given currency
        /// </summary>
        /// <param name="currencyCode">Currency code (for example, "rub")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of payment gateways</returns>
        Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysForCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the payment gateways that make payouts for the given currency
        /// </summary>
        /// <param name="currencyCode">Currency code (for example, "rub")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of payment gateways</returns>
        Task<IEnumerable<PaymentGatewayInfo>> GetPayoutGatewaysForCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current exchange rate of the given currency
        /// </summary>
        /// <param name="currencyCode">Currency code (for example, "rub")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Exchange rate or 0 if it could not be obtained</returns>
        Task<decimal> GetExchangeRateAsync(string currencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts an amount from a currency to the base currency at the current rate
        /// </summary>
        /// <param name="amount">Amount to convert</param>
        /// <param name="fromCurrencyCode">Code of the source currency</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Converted amount, or the original amount if the rate is not available</returns>
        Task<decimal> ConvertToBaseCurrencyAsync(decimal amount, string fromCurrencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts an amount from the base currency to the given currency at the current rate
        /// </summary>
        /// <param name="amount">Amount to convert</param>
        /// <param name="toCurrencyCode">Code of the target currency</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Converted amount, or the original amount if the rate is not available</returns>
        Task<decimal> ConvertFromBaseCurrencyAsync(decimal amount, string toCurrencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether the given currency is active
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the currency is active</returns>
        Task<bool> IsCurrencyActiveAsync(string currencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether the gateway accepts the given amount
        /// </summary>
        /// <param name="gatewayCode">Payment gateway code</param>
        /// <param name="amount">Amount to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the amount is within the allowed limits</returns>
        Task<bool> IsAmountValidForGatewayAsync(string gatewayCode, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the available payment detail types of the given gateway
        /// </summary>
        /// <param name="gatewayCode">Payment gateway code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available detail types or an empty list</returns>
        Task<IEnumerable<string>> GetDetailTypesForGatewayAsync(string gatewayCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether the gateway supports the given type of payment details
        /// </summary>
        /// <param name="gatewayCode">Payment gateway code</param>
        /// <param name="detailType">Detail type to check (for example, "card", "phone", "sbp")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the detail type is supported</returns>
        Task<bool> IsDetailTypeValidForGatewayAsync(string gatewayCode, string detailType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all catalog caches
        /// </summary>
        void ClearCache();
    }
}
