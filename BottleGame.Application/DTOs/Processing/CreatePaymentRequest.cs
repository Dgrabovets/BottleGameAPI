namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Request to create a payment (deposit) at the payment provider
    /// </summary>
    /// <param name="TransactionId">Id of the transaction in the application</param>
    /// <param name="Amount">Payment amount</param>
    /// <param name="Currency">Currency code</param>
    /// <param name="Description">Payment description</param>
    public record CreatePaymentRequest(int TransactionId, decimal Amount, string Currency, string Description);
}
