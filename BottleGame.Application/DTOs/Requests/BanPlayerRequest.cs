namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на бан игрока
    /// </summary>
    public class BanPlayerRequest
    {
        /// <summary>
        /// ID игрока для бана
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// Причина бана
        /// </summary>
        public string BanReason { get; set; } = string.Empty;
    }
}
