namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response with the profit for a period
    /// </summary>
    public class GetProfitResponse
    {
        /// <summary>
        /// Profit
        /// </summary>
        public decimal Profit { get; set; }

        /// <summary>
        /// Start date of the period
        /// </summary>
        public DateTime DateFrom { get; set; }

        /// <summary>
        /// End date of the period
        /// </summary>
        public DateTime DateTill { get; set; }
    }
}
