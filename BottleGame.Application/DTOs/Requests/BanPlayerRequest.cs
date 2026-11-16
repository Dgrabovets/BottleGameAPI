namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Request to ban a player
    /// </summary>
    public class BanPlayerRequest
    {
        /// <summary>
        /// Id of the player to ban
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// Reason for the ban
        /// </summary>
        public string BanReason { get; set; } = string.Empty;
    }
}
