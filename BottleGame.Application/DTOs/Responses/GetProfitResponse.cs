namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ с прибылью за период
    /// </summary>
    public class GetProfitResponse
    {
        /// <summary>
        /// Прибыль
        /// </summary>
        public decimal Profit { get; set; }

        /// <summary>
        /// Дата начала периода
        /// </summary>
        public DateTime DateFrom { get; set; }

        /// <summary>
        /// Дата конца периода
        /// </summary>
        public DateTime DateTill { get; set; }
    }
}
