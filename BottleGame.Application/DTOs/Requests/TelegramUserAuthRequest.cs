namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Telegram user authorization request
    /// </summary>
    public class TelegramUserAuthRequest
    {
        /// <summary>
        /// InitData from the Telegram Mini App
        /// </summary>
        public string InitData { get; set; } = string.Empty;
    }
}
