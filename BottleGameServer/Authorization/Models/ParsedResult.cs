namespace BottleGameServer.Authorization.Models
{
    /// <summary>
    /// Данные, прошедшие парсинг
    /// </summary>
    public class ParsedResult
    {
        /// <summary>
        /// ИД
        /// </summary>
        public long TelegramId { get; set; }

        /// <summary>
        /// Никнейм
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Ссылка на аватарку
        /// </summary>
        public string PhotoUrl { get; set; } = string.Empty;

        /// <summary>
        /// Дата регистрации в прилоожении
        /// </summary>
        public DateTime RegisteredAt { get; set; }

        /// <summary>
        /// ИД приглащающего игрока (опционально)
        /// </summary>
        public int? InvitingPlayerId { get; set; }
    }
}
