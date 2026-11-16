namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Request to create a money transaction
    /// </summary>
    public class MoneyTransactionRequest
    {
        /// <summary>
        /// Transaction amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Whether the transaction is a deposit (true - deposit, false - withdrawal)
        /// </summary>
        public bool IsDeposit { get; set; }
    }
}
