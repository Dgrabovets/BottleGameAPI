namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Point of the statistics timeline
    /// </summary>
    public class StatisticsTimelinePoint
    {
        /// <summary>
        /// Start of the bucket
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Cumulative number of players at the end of the bucket
        /// </summary>
        public int UsersCount { get; set; }

        /// <summary>
        /// Sum of deposits in the bucket
        /// </summary>
        public decimal DepositsAmount { get; set; }

        /// <summary>
        /// Sum of approved withdrawals in the bucket
        /// </summary>
        public decimal WithdrawalsAmount { get; set; }

        /// <summary>
        /// Income of the bucket (deposits − withdrawals)
        /// </summary>
        public decimal IncomeAmount { get; set; }
    }
}
