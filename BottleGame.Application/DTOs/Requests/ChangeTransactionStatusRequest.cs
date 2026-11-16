namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Request to change the status of a transaction
    /// </summary>
    public class ChangeTransactionStatusRequest
    {
        /// <summary>
        /// Id of the new transaction status
        /// </summary>
        public int TransactionStatusId { get; set; }
    }
}
