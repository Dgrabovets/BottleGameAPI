using BottleGame.Application.DTOs;

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

        /// <summary>
        /// Reads the user data from initData without checking the signature
        /// </summary>
        /// <param name="initData">initData as a query string</param>
        /// <returns>User data</returns>
        /// <remarks>
        /// Malformed data throws, so the caller must treat it as untrusted
        /// </remarks>
        ParsedTelegramUser ParseInitData(string initData);
    }
}
