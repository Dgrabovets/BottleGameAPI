namespace BottleGame.Application.DTOs
{
    /// <summary>
    /// Telegram user data taken from initData
    /// </summary>
    public class ParsedTelegramUser
    {
        /// <summary>
        /// Telegram id of the user
        /// </summary>
        public long TelegramId { get; set; }

        /// <summary>
        /// Username of the user
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Link to the user's avatar
        /// </summary>
        public string PhotoUrl { get; set; } = string.Empty;

        /// <summary>
        /// Date of registration in the application
        /// </summary>
        public DateTime RegisteredAt { get; set; }

        /// <summary>
        /// Id of the inviting player (optional)
        /// </summary>
        public int? InvitingPlayerId { get; set; }
    }
}
