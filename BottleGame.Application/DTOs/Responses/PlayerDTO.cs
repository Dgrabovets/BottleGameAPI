namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Player DTO
    /// </summary>
    public class PlayerDTO
    {
        /// <summary>
        /// Unique identifier of the player
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Telegram id of the player
        /// </summary>
        public long TelegramId { get; set; }

        /// <summary>
        /// Player name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Avatar URL of the player
        /// </summary>
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Date of registration in the application
        /// </summary>
        public DateTime RegisteredInAppAt { get; set; }

        /// <summary>
        /// Win percentage of the player
        /// </summary>
        public double WinRate { get; set; }
    }
}
