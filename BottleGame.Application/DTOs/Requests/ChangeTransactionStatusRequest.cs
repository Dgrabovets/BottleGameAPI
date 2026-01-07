namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на изменение статуса транзакции
    /// </summary>
    public class ChangeTransactionStatusRequest
    {
        /// <summary>
        /// ID нового статуса транзакции
        /// </summary>
        public int TransactionStatusId { get; set; }
    }
}
