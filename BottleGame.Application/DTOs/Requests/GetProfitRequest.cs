namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на получение прибыли за период
    /// </summary>
    public class GetProfitRequest
    {
        /// <summary>
        /// Дата начала периода (необязательно)
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Дата конца периода (необязательно)
        /// </summary>
        public DateTime? DateTill { get; set; }
    }
}
