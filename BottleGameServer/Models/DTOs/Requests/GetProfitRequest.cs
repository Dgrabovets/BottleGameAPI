namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса на получение фин. показателей приложения
    /// </summary>
    public class GetProfitRequest
    {
        /// <summary>
        /// Нижний порог периода даты
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Верхний порог периода даты
        /// </summary>
        public DateTime? DateTill { get; set; }
    }
}
