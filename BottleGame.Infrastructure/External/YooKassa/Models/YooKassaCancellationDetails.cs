using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Reason for canceling a payment or payout
    /// </summary>
    internal class YooKassaCancellationDetails
    {
        /// <summary>
        /// Who canceled the operation
        /// </summary>
        [JsonPropertyName("party")]
        public string? Party { get; set; }

        /// <summary>
        /// Reason code
        /// </summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
