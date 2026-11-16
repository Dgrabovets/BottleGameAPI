namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Statistics timeline for charts
    /// </summary>
    public class GetAppStatisticsTimelineResponse
    {
        /// <summary>
        /// Start of the requested period
        /// </summary>
        public DateTime DateFrom { get; set; }

        /// <summary>
        /// End of the requested period
        /// </summary>
        public DateTime DateTill { get; set; }

        /// <summary>
        /// Bucket size: day, week or month
        /// </summary>
        public string Bucket { get; set; } = "day";

        /// <summary>
        /// Points of the timeline
        /// </summary>
        public List<StatisticsTimelinePoint> Points { get; set; } = [];
    }
}
