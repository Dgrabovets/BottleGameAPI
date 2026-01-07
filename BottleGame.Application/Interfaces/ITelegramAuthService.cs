namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Сервис для авторизации Telegram-пользователей
    /// </summary>
    public interface ITelegramAuthService
    {
        /// <summary>
        /// Валидирует initData от Telegram
        /// </summary>
        bool ValidateInitData(string initData);
    }
}

