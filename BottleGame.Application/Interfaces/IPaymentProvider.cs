using BottleGame.Application.DTOs.Processing;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Payment provider: accepting payments, payouts and catalogs
    /// </summary>
    public interface IPaymentProvider
    {
        /// <summary>
        /// Returns the currencies the provider works with
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of currencies</returns>
        Task<IEnumerable<CurrencyInfo>> GetCurrenciesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the ways to accept payments and make payouts
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of methods</returns>
        Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the rate of a currency to the base currency of the application
        /// </summary>
        /// <param name="currency">Currency code</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rate; 0 if the currency is not supported</returns>
        Task<decimal> GetExchangeRateAsync(string currency, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a payment. A repeated call for the same transaction returns the same payment
        /// </summary>
        /// <param name="request">Request to create a payment</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of creating the payment</returns>
        Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a payout. A repeated call for the same transaction returns the same payout
        /// </summary>
        /// <param name="request">Request to create a payout</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of creating the payout</returns>
        Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asks the provider for the current state of a payment
        /// </summary>
        /// <param name="paymentId">Id of the payment at the provider</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The payment, or null if the provider does not know it</returns>
        Task<ProviderOperation?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asks the provider for the current state of a payout
        /// </summary>
        /// <param name="payoutId">Id of the payout at the provider</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The payout, or null if the provider does not know it</returns>
        Task<ProviderOperation?> GetPayoutAsync(string payoutId, CancellationToken cancellationToken = default);
    }
}
