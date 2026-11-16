namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Result of creating a payout
    /// </summary>
    /// <param name="IsSuccess">The payout was created</param>
    /// <param name="PayoutId">Id of the payout at the provider</param>
    /// <param name="Error">Error description</param>
    public record CreatePayoutResult(bool IsSuccess, string? PayoutId, string? Error);
}
