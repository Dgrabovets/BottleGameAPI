using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Дочерняя модель ответа плучения всех транзакций пользователя, содержащая данные об игроке
    /// </summary>
    public class PlayerDTO
    {
        /// <summary>
        /// ИД
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Телеграм ИД
        /// </summary>
        public long TelegramId { get; set; }

        /// <summary>
        /// Никнейм
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Ссылка на аватарку
        /// </summary>
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Дата регистрации в приложении
        /// </summary>
        public DateTime RegisteredInAppAt { get; set; }

        /// <summary>
        /// Процент побед
        /// </summary>
        public double WinRate { get; set; }
    }
}
