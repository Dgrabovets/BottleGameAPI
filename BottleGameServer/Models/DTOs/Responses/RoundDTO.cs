namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа, содержащая данные о раунде
    /// </summary>
    public class RoundDTO
    {
        /// <summary>
        /// Идентификатор раунда 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ставка
        /// </summary>
        public int Bet { get; set; }

        /// <summary>
        /// Признак, выиграл ли игрок
        /// </summary>
        public bool IsWin { get; set; }

        /// <summary>
        /// Время старта раунда
        /// </summary>
        public DateTime FinishedAt { get; set; }
    }
}
