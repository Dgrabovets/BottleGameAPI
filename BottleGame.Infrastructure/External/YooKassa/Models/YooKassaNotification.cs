using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Incoming YooKassa notification
    /// </summary>
    internal class YooKassaNotification
    {
        /// <summary>
        /// Event name, for example payment.succeeded
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        /// <summary>
        /// Object the event refers to
        /// </summary>
        [JsonPropertyName("object")]
        public YooKassaOperation? Object { get; set; }
    }
}
