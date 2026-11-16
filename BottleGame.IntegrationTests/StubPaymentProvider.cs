using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;

namespace BottleGame.IntegrationTests;

/// <summary>
/// Payment provider that answers a payment lookup with a prepared success; with several participants the answer is given only after all of them have asked, so parallel handlers reach the database together.
/// </summary>
/// <param name="paymentId">Payment id.</param>
/// <param name="transactionId">Transaction id.</param>
/// <param name="amount">Amount.</param>
/// <param name="rendezvousParticipants">Rendezvous participants.</param>
public sealed class StubPaymentProvider(string paymentId, int transactionId, decimal amount, int rendezvousParticipants = 1) : IPaymentProvider
{
    /// <summary>
    /// Barrier.
    /// </summary>
    private readonly Barrier? _barrier = rendezvousParticipants > 1 ? new Barrier(rendezvousParticipants) : null;

    /// <summary>
    /// Returns the configured payment; with several participants it first waits for all of them.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ProviderOperation?> GetPaymentAsync(string id, CancellationToken cancellationToken = default)
    {
        _barrier?.SignalAndWait(TimeSpan.FromSeconds(10), cancellationToken);
        return Task.FromResult<ProviderOperation?>(
            new ProviderOperation(paymentId, ProviderOperationStatus.Succeeded, amount, "rub", transactionId, null));
    }

    /// <summary>
    /// Not used by these tests.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IEnumerable<CurrencyInfo>> GetCurrenciesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by these tests.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by these tests.
    /// </summary>
    /// <param name="currency">Currency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public Task<decimal> GetExchangeRateAsync(string currency, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by these tests.
    /// </summary>
    /// <param name="request">Request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by these tests.
    /// </summary>
    /// <param name="request">Request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Not used by these tests.
    /// </summary>
    /// <param name="payoutId">Payout id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ProviderOperation?> GetPayoutAsync(string payoutId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
