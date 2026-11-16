namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// A payment or payout as the provider sees it
    /// </summary>
    /// <param name="Id">Id at the provider</param>
    /// <param name="Status">State</param>
    /// <param name="Amount">Amount</param>
    /// <param name="Currency">Currency code</param>
    /// <param name="TransactionId">Id of the transaction in the application, saved at creation</param>
    /// <param name="CancellationReason">Reason for the cancellation, if the operation was canceled</param>
    public record ProviderOperation(
        string Id,
        ProviderOperationStatus Status,
        decimal Amount,
        string Currency,
        int? TransactionId,
        string? CancellationReason);
}
