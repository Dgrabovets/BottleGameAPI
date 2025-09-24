namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса на изменение статуса транзакции
    /// </summary>
    public class ChangeTransactionStatusRequest
    {
        /// <summary>
        /// ИД статуса транзакции
        /// </summary>
        public int TransactionStatusId { get; set; }
    }
}
