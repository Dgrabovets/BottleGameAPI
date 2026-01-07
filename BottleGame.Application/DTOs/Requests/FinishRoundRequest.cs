namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на завершение раунда
    /// </summary>
    public class FinishRoundRequest
    {
        /// <summary>
        /// Размер ставки
        /// </summary>
        public decimal Bet { get; set; }

        /// <summary>
        /// Признак выигрыша
        /// </summary>
        public bool IsWin { get; set; }
    }
}
