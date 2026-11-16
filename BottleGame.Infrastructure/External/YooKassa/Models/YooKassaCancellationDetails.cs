using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Причина отмены платежа или выплаты.
    /// </summary>
    internal class YooKassaCancellationDetails
    {
        /// <summary>
        /// Кто отменил операцию.
        /// </summary>
        [JsonPropertyName("party")]
        public string? Party { get; set; }

        /// <summary>
        /// Код причины.
        /// </summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
