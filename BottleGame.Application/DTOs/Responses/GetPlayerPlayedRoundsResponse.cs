namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response with the list of rounds a player played
    /// </summary>
    public class GetPlayerPlayedRoundsResponse
    {
        /// <summary>
        /// Player data
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// List of rounds
        /// </summary>
        public List<RoundDTO> Rounds { get; set; } = [];
    }
}
