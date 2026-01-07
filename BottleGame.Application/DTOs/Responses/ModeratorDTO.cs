namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// DTO модератора
    /// </summary>
    public class ModeratorDTO
    {
        /// <summary>
        /// Уникальный идентификатор модератора
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Логин модератора
        /// </summary>
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
