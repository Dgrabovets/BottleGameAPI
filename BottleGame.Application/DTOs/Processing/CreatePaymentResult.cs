namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Result of creating a payment
    /// </summary>
    /// <param name="IsSuccess">The payment was created</param>
    /// <param name="PaymentId">Id of the payment at the provider</param>
    /// <param name="PaymentUrl">Link the player must be sent to in order to pay</param>
    /// <param name="Error">Error description</param>
    public record CreatePaymentResult(bool IsSuccess, string? PaymentId, string? PaymentUrl, string? Error);
}
