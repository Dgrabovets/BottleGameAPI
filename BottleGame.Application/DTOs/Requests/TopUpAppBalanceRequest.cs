namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на пополнение баланса приложения
    /// </summary>
    public class TopUpAppBalanceRequest
    {
        /// <summary>
        /// Сумма пополнения
        /// </summary>
        public decimal Amount { get; set; }
    }
}
