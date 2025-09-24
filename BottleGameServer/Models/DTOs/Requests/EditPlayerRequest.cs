namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса на изменение игрока
    /// </summary>
    public class EditPlayerRequest
    {
        /// <summary>
        /// Имя
        /// </summary>
        public string? Name { get; set; } = string.Empty;

        /// <summary>
        /// Ссылка на аватарку
        /// </summary>
        public string? AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Шанс выигрыша для игрока
        /// </summary>
        public float? WinChance { get; set; }
    }
}
