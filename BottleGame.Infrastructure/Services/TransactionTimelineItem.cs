namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Simplified representation of a transaction for the timeline aggregation
    /// </summary>
    /// <param name="TypeId">Transaction type identifier</param>
    /// <param name="Amount">Transaction amount</param>
    /// <param name="EffectiveAt">Date of successful processing</param>
    internal sealed record TransactionTimelineItem(
        int TypeId,
        decimal Amount,
        DateTime EffectiveAt);
}
