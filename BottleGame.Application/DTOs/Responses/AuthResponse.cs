namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ авторизации
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// Идентификатор игрока
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// JWT токен доступа
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}
