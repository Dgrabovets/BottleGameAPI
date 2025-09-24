using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса для авторизации админа
    /// </summary>
    public class AuthAdminRequest
    {
        /// <summary>
        /// Логин
        /// </summary>
        [Required]
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Пароль
        /// </summary>
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
