namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа получения баланса игрока
    /// </summary>
    public class GetPlayerBalanceResponse
    {
        /// <summary>
        /// ИД игрока
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// Баланс
        /// </summary>
        public int Balance { get; set; }
    }
}
