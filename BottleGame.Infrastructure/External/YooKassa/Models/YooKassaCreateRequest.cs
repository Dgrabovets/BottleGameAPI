using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Body of the request to create a payment or payout
    /// </summary>
    internal class YooKassaCreateRequest
    {
        /// <summary>
        /// Amount
        /// </summary>
        [JsonPropertyName("amount")]
        public YooKassaAmount Amount { get; set; } = new();

        /// <summary>
        /// Description of the operation
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Arbitrary data that YooKassa returns together with the object
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = [];

        /// <summary>
        /// Flag of automatic capture (payment only)
        /// </summary>
        [JsonPropertyName("capture")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Capture { get; set; }

        /// <summary>
        /// Confirmation of the payment by the player (payment only)
        /// </summary>
        [JsonPropertyName("confirmation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public YooKassaConfirmation? Confirmation { get; set; }

        /// <summary>
        /// Payout recipient (payout only)
        /// </summary>
        [JsonPropertyName("payout_destination_data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public YooKassaPayoutDestination? PayoutDestination { get; set; }
    }
}
