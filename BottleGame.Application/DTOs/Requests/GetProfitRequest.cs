namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Request to get the profit for a period
    /// </summary>
    public class GetProfitRequest
    {
        /// <summary>
        /// Start date of the period (optional)
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// End date of the period (optional)
        /// </summary>
        public DateTime? DateTill { get; set; }
    }
}
