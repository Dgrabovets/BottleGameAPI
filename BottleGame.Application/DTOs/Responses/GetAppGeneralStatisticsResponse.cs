namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Statistics for the administrator
    /// </summary>
    public class GetAppGeneralStatisticsResponse
    {
        /// <summary>
        /// Total number of users
        /// </summary>
        public int UsersTotal { get; set; }

        /// <summary>
        /// Sum of successfully processed deposits
        /// </summary>
        public decimal DepositsTotal { get; set; }

        /// <summary>
        /// Total of the player balances (only for the general statistics without a period)
        /// </summary>
        public decimal? BalancesTotal { get; set; }

        /// <summary>
        /// Sum of successfully processed withdrawals
        /// </summary>
        public decimal WithdrawalsTotal { get; set; }

        /// <summary>
        /// Income: successfully processed deposits minus successfully processed withdrawals
        /// </summary>
        public decimal IncomeTotal { get; set; }

        /// <summary>
        /// Sum of withdrawals being processed (status Pending)
        /// </summary>
        public decimal PendingWithdrawalsTotal { get; set; }
    }
}
