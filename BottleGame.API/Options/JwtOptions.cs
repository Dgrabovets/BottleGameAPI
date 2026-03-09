using System.ComponentModel.DataAnnotations;

namespace BottleGame.API.Options
{
    /// <summary>
    /// Настройки JWT-аутентификации.
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// Секретный ключ для подписи токенов (минимум 32 символа).
        /// Задаётся через конфигурацию или переменную окружения JWT__Key.
        /// </summary>
        [Required(ErrorMessage = "JWT:Key обязателен. Задайте переменную окружения JWT__Key или секрет JWT_KEY в GitHub Actions.")]
        [MinLength(32, ErrorMessage = "JWT:Key должен быть не короче 32 символов.")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Издатель токена.
        /// </summary>
        public string Issuer { get; set; } = "BottleAppAPI";

        /// <summary>
        /// Аудитория токена.
        /// </summary>
        public string Audience { get; set; } = "BottleGame";

        /// <summary>
        /// Время жизни токена в минутах.
        /// </summary>
        [Range(1, 10080)]
        public int ExpireMinutes { get; set; } = 360;
    }
}
