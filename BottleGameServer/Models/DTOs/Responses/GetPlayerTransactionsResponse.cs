using BottleGameServer.Models.Entities;

namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа получения транзакций игрока
    /// </summary>
    public class GetPlayerTransactionsResponse
    {
        /// <summary>
        /// Данные об игроке
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Данные о транзакциях
        /// </summary>
        public IEnumerable<TransactionDTO> Transactions { get; set; } = null!;
    }
}
