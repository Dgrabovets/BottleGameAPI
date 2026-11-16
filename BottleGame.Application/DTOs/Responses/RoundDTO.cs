namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Game round DTO
    /// </summary>
    public class RoundDTO
    {
        /// <summary>
        /// Unique identifier of the round
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Bet in the round
        /// </summary>
        public decimal Bet { get; set; }

        /// <summary>
        /// Whether the round was won
        /// </summary>
        public bool IsWin { get; set; }

        /// <summary>
        /// Date the round finished
        /// </summary>
        public DateTime FinishedAt { get; set; }
    }
}
