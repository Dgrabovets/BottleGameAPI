using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса авторизации игрока
    /// </summary>
    public class TelegramUserAuthRequest
    {
        /// <summary>
        /// Телеграм ИД
        /// </summary>
        [Required]
        public long TelegramId { get; set; }

        /// <summary>
        /// Никнейм
        /// </summary>
        [Required]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Ссылка на аватарку
        /// </summary>
        [Required]
        public string PhotoUrl { get; set; } = string.Empty;

        /// <summary>
        /// Дата регистрации в телеграме
        /// </summary>
        [Required]
        public DateTime RegisteredAt { get; set; }

        /// <summary>
        /// Хэш
        /// </summary>
        [Required]
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// Стартовый параметр
        /// </summary>
        public string StartParam { get; set; } = string.Empty;
    }
}
