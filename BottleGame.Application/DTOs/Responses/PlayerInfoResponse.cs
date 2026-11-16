namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Information about a player
    /// </summary>
    public class PlayerInfoResponse
    {
        /// <summary>
        /// Player data
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Player balance
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Rounds of the player
        /// </summary>
        public IEnumerable<RoundDTO> Rounds { get; set; } = [];

        /// <summary>
        /// Ban date (if banned)
        /// </summary>
        public DateTime? BannedAt { get; set; }

        /// <summary>
        /// Ids of the invited players
        /// </summary>
        public IEnumerable<int> PlayersIdsInvited { get; set; } = null!;
    }
}
