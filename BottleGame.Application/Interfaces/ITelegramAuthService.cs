namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Service for authorizing Telegram users
    /// </summary>
    public interface ITelegramAuthService
    {
        /// <summary>
        /// Validates the initData from Telegram
        /// </summary>
        bool ValidateInitData(string initData);
    }
}
