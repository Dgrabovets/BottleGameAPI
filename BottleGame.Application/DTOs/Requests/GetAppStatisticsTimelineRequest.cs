namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Statistics timeline request
    /// </summary>
    public class GetAppStatisticsTimelineRequest
    {
        /// <summary>
        /// Start of the period (inclusive, UTC)
        /// </summary>
        public DateTime DateFrom { get; set; }

        /// <summary>
        /// End of the period (inclusive, UTC)
        /// </summary>
        public DateTime DateTill { get; set; }
    }
}
