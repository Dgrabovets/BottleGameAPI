namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Player statistics
    /// </summary>
    public class PlayerStatisticsResponse
    {
        /// <summary>
        /// Player data
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Ban date (if banned)
        /// </summary>
        public DateTime? BannedAt { get; set; }

        /// <summary>
        /// Current balance
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Number of rounds played
        /// </summary>
        public int RoundsQty { get; set; }

        /// <summary>
        /// Number of rounds won
        /// </summary>
        public int WinsQty { get; set; }

        /// <summary>
        /// Number of rounds lost
        /// </summary>
        public int LossQty { get; set; }

        /// <summary>
        /// Sum of winnings
        /// </summary>
        public decimal WinAmount { get; set; }

        /// <summary>
        /// Sum of losses
        /// </summary>
        public decimal LossAmount { get; set; }
    }
}
