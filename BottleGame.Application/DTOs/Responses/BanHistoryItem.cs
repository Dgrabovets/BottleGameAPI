namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Элемент истории банов
    /// </summary>
    public class BanHistoryItem
    {
        /// <summary>
        /// Причина бана
        /// </summary>
        public string BanReason { get; set; } = string.Empty;

        /// <summary>
        /// Дата бана
        /// </summary>
        public DateTime BannedAt { get; set; }

        /// <summary>
        /// Дата разбана
        /// </summary>
        public DateTime? UnbannedAt { get; set; }
    }
}

