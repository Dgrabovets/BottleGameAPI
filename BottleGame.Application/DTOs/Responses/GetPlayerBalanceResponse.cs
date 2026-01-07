namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ с балансом игрока
    /// </summary>
    public class GetPlayerBalanceResponse
    {
        /// <summary>
        /// Идентификатор игрока
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// Баланс игрока
        /// </summary>
        public decimal Balance { get; set; }
    }
}
