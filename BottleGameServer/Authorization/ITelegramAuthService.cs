using BottleGameServer.Models.DTOs.Requests;

namespace BottleGameServer.Authorization
{
    /// <summary>
    /// Служит для авторизации телеграм-пользователей
    /// </summary>
    public interface ITelegramAuthService
    {
        /// <summary>
        /// Валидирует Hash
        /// </summary>
        bool ValidateInitData(string initData);
    }
}
