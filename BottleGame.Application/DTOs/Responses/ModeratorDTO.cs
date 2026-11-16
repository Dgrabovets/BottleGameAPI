namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Moderator DTO
    /// </summary>
    public class ModeratorDTO
    {
        /// <summary>
        /// Unique identifier of the moderator
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Moderator login
        /// </summary>
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
