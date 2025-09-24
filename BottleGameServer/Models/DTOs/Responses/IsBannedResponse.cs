namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа забанен ли пользователь
    /// </summary>
    public class IsBannedResponse
    {
        /// <summary>
        /// Признак, забанен ли пользователь
        /// </summary>
        public bool IsBanned { get; set; }

        /// <summary>
        /// Причина запрета
        /// </summary>
        public string BanReason { get; set; } = string.Empty;

        /// <summary>
        /// История банов пользователя
        /// </summary>
        public List<BanHistoryItem> BanHistory { get; set; } = new List<BanHistoryItem>();
    }

    /// <summary>
    /// Элемент истории бана
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
        /// Дата разбана (null если бан активен)
        /// </summary>
        public DateTime? UnbannedAt { get; set; }
    }
}
