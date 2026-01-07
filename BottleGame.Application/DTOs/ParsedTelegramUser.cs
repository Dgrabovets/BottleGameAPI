namespace BottleGame.Application.DTOs
{
    /// <summary>
    /// Данные пользователя Telegram, полученные из initData
    /// </summary>
    public class ParsedTelegramUser
    {
        /// <summary>
        /// Telegram ID пользователя
        /// </summary>
        public long TelegramId { get; set; }

        /// <summary>
        /// Username пользователя
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Ссылка на аватар пользователя
        /// </summary>
        public string PhotoUrl { get; set; } = string.Empty;

        /// <summary>
        /// Дата регистрации в приложении
        /// </summary>
        public DateTime RegisteredAt { get; set; }

        /// <summary>
        /// ID пригласившего игрока (опционально)
        /// </summary>
        public int? InvitingPlayerId { get; set; }
    }
}

