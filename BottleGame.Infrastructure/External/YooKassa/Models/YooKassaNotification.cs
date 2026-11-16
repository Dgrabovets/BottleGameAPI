using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Входящее уведомление ЮKassa.
    /// </summary>
    internal class YooKassaNotification
    {
        /// <summary>
        /// Название события, например payment.succeeded.
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        /// <summary>
        /// Объект, к которому относится событие.
        /// </summary>
        [JsonPropertyName("object")]
        public YooKassaOperation? Object { get; set; }
    }
}
