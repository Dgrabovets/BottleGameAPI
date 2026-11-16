namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Request to create a payout (withdrawal) at the payment provider
    /// </summary>
    /// <param name="TransactionId">Id of the transaction in the application</param>
    /// <param name="Amount">Payout amount</param>
    /// <param name="Currency">Currency code</param>
    /// <param name="Gateway">Code of the payout method</param>
    /// <param name="Destination">Recipient details in the format of the chosen payout method</param>
    /// <param name="Description">Payout description</param>
    public record CreatePayoutRequest(int TransactionId, decimal Amount, string Currency, string Gateway, string Destination, string Description);
}
