namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос временной шкалы статистики
    /// </summary>
    public class GetAppStatisticsTimelineRequest
    {
        /// <summary>
        /// Начало периода (включительно, UTC)
        /// </summary>
        public DateTime DateFrom { get; set; }

        /// <summary>
        /// Конец периода (включительно, UTC)
        /// </summary>
        public DateTime DateTill { get; set; }
    }
}
