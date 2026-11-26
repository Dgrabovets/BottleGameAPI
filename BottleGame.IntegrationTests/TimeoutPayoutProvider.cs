using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;

namespace BottleGame.IntegrationTests;

/// <summary>
/// Payment provider whose payout request runs a prepared action while it is in flight and then times out, so a test can act during the request
/// </summary>
/// <param name="duringRequest">Action run while the payout request is in flight</param>
public sealed class TimeoutPayoutProvider(Func<Task> duringRequest) : IPaymentProvider
{
    /// <summary>
    /// Runs the prepared action, then fails as a timeout does, so the outcome of the payout stays unknown
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Never returns, always throws</returns>
    public async Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default)
    {
        await duringRequest();
        throw new TaskCanceledException("The payout request timed out");
    }

    /// <summary>
    /// Not used by the tests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    public Task<IEnumerable<CurrencyInfo>> GetCurrenciesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by the tests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    public Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by the tests
    /// </summary>
    /// <param name="currency">Currency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    public Task<decimal> GetExchangeRateAsync(string currency, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by the tests
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    public Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by the tests
    /// </summary>
    /// <param name="paymentId">Payment id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    public Task<ProviderOperation?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by the tests
    /// </summary>
    /// <param name="payoutId">Payout id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    public Task<ProviderOperation?> GetPayoutAsync(string payoutId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
