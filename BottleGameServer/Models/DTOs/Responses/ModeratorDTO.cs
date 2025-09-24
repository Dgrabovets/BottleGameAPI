namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель учетной записи модератора
    /// </summary>
    public class ModeratorDTO
    {
        /// <summary>
        /// Идентификатор модератора
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Логин
        /// </summary>
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания учетной записи
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Роль (1 - админ, 2 - модератор)
        /// </summary>
        public int Role { get; set; }
    }
}
