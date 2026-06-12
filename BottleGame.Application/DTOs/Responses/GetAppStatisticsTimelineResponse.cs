namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Временная шкала статистики для графиков
    /// </summary>
    public class GetAppStatisticsTimelineResponse
    {
        /// <summary>
        /// Начало запрошенного периода
        /// </summary>
        public DateTime DateFrom { get; set; }

        /// <summary>
        /// Конец запрошенного периода
        /// </summary>
        public DateTime DateTill { get; set; }

        /// <summary>
        /// Размер бакета: day, week или month
        /// </summary>
        public string Bucket { get; set; } = "day";

        /// <summary>
        /// Точки шкалы
        /// </summary>
        public List<StatisticsTimelinePoint> Points { get; set; } = [];
    }
}
