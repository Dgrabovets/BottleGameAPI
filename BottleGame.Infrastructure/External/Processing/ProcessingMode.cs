namespace BottleGame.Infrastructure.External.Processing
{
    /// <summary>
    /// Mode of working with the payment processing
    /// </summary>
    public enum ProcessingMode
    {
        /// <summary>
        /// Deposits and withdrawals complete locally without calling the processing and without real money.
        /// For development, tests and demonstrations
        /// </summary>
        Simulated,

        /// <summary>
        /// Payments and payouts go through the payment processing, statuses arrive by webhooks
        /// </summary>
        Live
    }
}
