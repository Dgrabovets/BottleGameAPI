namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа аутентификации
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// ИД игрока
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// Полученный токен
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}
