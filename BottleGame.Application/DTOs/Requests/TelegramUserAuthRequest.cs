namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос авторизации пользователя Telegram
    /// </summary>
    public class TelegramUserAuthRequest
    {
        /// <summary>
        /// InitData от Telegram Mini App
        /// </summary>
        public string InitData { get; set; } = string.Empty;
    }
}
