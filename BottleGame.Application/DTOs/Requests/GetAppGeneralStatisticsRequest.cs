namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Administration statistics request with an optional period
    /// </summary>
    public class GetAppGeneralStatisticsRequest
    {
        /// <summary>
        /// Start of the period (inclusive, UTC)
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// End of the period (inclusive, UTC)
        /// </summary>
        public DateTime? DateTill { get; set; }
    }
}
