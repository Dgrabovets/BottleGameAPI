namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Request to finish a round
    /// </summary>
    public class FinishRoundRequest
    {
        /// <summary>
        /// Bet size
        /// </summary>
        public decimal Bet { get; set; }

        /// <summary>
        /// Whether the round was won
        /// </summary>
        public bool IsWin { get; set; }
    }
}
