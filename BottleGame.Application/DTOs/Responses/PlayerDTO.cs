namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// DTO игрока
    /// </summary>
    public class PlayerDTO
    {
        /// <summary>
        /// Уникальный идентификатор игрока
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Telegram ID игрока
        /// </summary>
        public long TelegramId { get; set; }

        /// <summary>
        /// Имя игрока
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL аватара игрока
        /// </summary>
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Дата регистрации в приложении
        /// </summary>
        public DateTime RegisteredInAppAt { get; set; }

        /// <summary>
        /// Процент побед игрока
        /// </summary>
        public double WinRate { get; set; }
    }
}
