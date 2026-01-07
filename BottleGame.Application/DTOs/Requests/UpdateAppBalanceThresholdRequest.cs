namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на обновление порога баланса приложения
    /// </summary>
    public class UpdateAppBalanceThresholdRequest
    {
        /// <summary>
        /// Новый нижний порог
        /// </summary>
        public decimal LowerThreshold { get; set; }
    }
}
