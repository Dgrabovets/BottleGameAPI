namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Information about a player by id
    /// </summary>
    public class PlayerInfoByIdResponse
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
        /// Player settings
        /// </summary>
        public PlayerOptionsDTO Options { get; set; } = null!;

        /// <summary>
        /// Invited players
        /// </summary>
        public IEnumerable<PlayerDTO> PlayersInvited { get; set; } = [];
    }
}
