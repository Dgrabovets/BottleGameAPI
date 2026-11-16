namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response about the ban status
    /// </summary>
    public class IsBannedResponse
    {
        /// <summary>
        /// Whether the player is banned
        /// </summary>
        public bool IsBanned { get; set; }

        /// <summary>
        /// Reason for the ban (if banned)
        /// </summary>
        public string BanReason { get; set; } = string.Empty;

        /// <summary>
        /// Ban history
        /// </summary>
        public List<BanHistoryItem> BanHistory { get; set; } = [];
    }
}
