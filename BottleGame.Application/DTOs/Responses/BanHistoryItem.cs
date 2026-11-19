namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ban history item
    /// </summary>
    public class BanHistoryItem
    {
        /// <summary>
        /// Reason for the ban
        /// </summary>
        public string BanReason { get; set; } = string.Empty;

        /// <summary>
        /// Ban date
        /// </summary>
        public DateTime BannedAt { get; set; }

        /// <summary>
        /// Unban date
        /// </summary>
        public DateTime? UnbannedAt { get; set; }
    }
}
