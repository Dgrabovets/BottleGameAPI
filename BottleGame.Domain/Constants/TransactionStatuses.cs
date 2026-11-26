namespace BottleGame.Domain.Constants
{
    /// <summary>
    /// Statuses of money transactions
    /// </summary>
    public static class TransactionStatuses
    {
        /// <summary>
        /// Waiting to be processed
        /// </summary>
        public const int PENDING = 1;

        /// <summary>
        /// Approved / Successful
        /// </summary>
        public const int APPROVED = 2;

        /// <summary>
        /// Rejected / Failed
        /// </summary>
        public const int REJECTED = 3;

        /// <summary>
        /// Status names for logging
        /// </summary>
        public static string GetName(int statusId) => statusId switch
        {
            PENDING => "Pending",
            APPROVED => "Approved",
            REJECTED => "Rejected",
            _ => "Unknown status"
        };
    }
}
