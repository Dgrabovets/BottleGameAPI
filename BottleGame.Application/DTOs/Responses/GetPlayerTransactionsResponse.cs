namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response with the list of a player's transactions
    /// </summary>
    public class GetPlayerTransactionsResponse
    {
        /// <summary>
        /// Player data
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// List of transactions
        /// </summary>
        public List<TransactionDTO> Transactions { get; set; } = [];
    }
}
