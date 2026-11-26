namespace BottleGame.Domain.Enums
{
    /// <summary>
    /// State of the payout of a withdrawal at the payment provider.
    /// The numbers are stored in the database, so they must never change
    /// </summary>
    public enum PayoutState
    {
        /// <summary>
        /// No payout request is recorded
        /// </summary>
        NotRequested = 0,

        /// <summary>
        /// The request was sent or is about to be sent and its outcome is not known, so the payout may exist
        /// </summary>
        OutcomeUnknown = 1,

        /// <summary>
        /// The provider accepted the request or reported the payout as succeeded
        /// </summary>
        Completed = 2,

        /// <summary>
        /// The provider refused the request or canceled the payout
        /// </summary>
        Refused = 3
    }
}
