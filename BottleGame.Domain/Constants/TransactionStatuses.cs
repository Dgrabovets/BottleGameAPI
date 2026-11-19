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
        public const int Pending = 1;

        /// <summary>
        /// Approved / Successful
        /// </summary>
        public const int Approved = 2;

        /// <summary>
        /// Rejected / Failed
        /// </summary>
        public const int Rejected = 3;

        /// <summary>
        /// Status names for logging
        /// </summary>
        public static string GetName(int statusId) => statusId switch
        {
            Pending => "Pending",
            Approved => "Approved",
            Rejected => "Rejected",
            _ => "Unknown status"
        };
    }
}
