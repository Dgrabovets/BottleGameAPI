namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// DTO игрового раунда
    /// </summary>
    public class RoundDTO
    {
        /// <summary>
        /// Уникальный идентификатор раунда
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ставка в раунде
        /// </summary>
        public decimal Bet { get; set; }

        /// <summary>
        /// Выигран ли раунд
        /// </summary>
        public bool IsWin { get; set; }

        /// <summary>
        /// Дата завершения раунда
        /// </summary>
        public DateTime FinishedAt { get; set; }
    }
}
