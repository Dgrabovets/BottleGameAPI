namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ о статусе бана
    /// </summary>
    public class IsBannedResponse
    {
        /// <summary>
        /// Забанен ли игрок
        /// </summary>
        public bool IsBanned { get; set; }

        /// <summary>
        /// Причина бана (если забанен)
        /// </summary>
        public string BanReason { get; set; } = string.Empty;

        /// <summary>
        /// История банов
        /// </summary>
        public List<BanHistoryItem> BanHistory { get; set; } = [];
    }
}
