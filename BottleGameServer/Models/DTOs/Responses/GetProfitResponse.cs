namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа на запрос получения прибыли приложения
    /// </summary>
    public class GetProfitResponse
    {
        /// <summary>
        /// Прибыль
        /// </summary>
        public int Profit { get; set; }

        /// <summary>
        /// Нижний порог периода даты
        /// </summary>
        public DateTime DateFrom { get; set; }

        /// <summary>
        /// Верхний порог периода даты
        /// </summary>
        public DateTime DateTill { get; set; }
    }
}
