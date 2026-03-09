namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ со списком транзакций игрока
    /// </summary>
    public class GetPlayerTransactionsResponse
    {
        /// <summary>
        /// Данные игрока
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Список транзакций
        /// </summary>
        public List<TransactionDTO> Transactions { get; set; } = [];
    }
}
