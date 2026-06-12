namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос админской статистики с опциональным периодом
    /// </summary>
    public class GetAppGeneralStatisticsRequest
    {
        /// <summary>
        /// Начало периода (включительно, UTC)
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Конец периода (включительно, UTC)
        /// </summary>
        public DateTime? DateTill { get; set; }
    }
}
