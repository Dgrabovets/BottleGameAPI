namespace BottleGame.API.Options
{
    /// <summary>
    /// Telegram settings checked at startup
    /// </summary>
    public class TelegramOptions
    {
        /// <summary>
        /// Token of the Telegram bot used to verify initData. Set through the TelegramBotToken environment variable
        /// </summary>
        public string BotToken { get; set; } = string.Empty;
    }
}
