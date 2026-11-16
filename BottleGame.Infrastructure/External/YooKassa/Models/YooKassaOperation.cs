using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Payment or payout in the YooKassa response
    /// </summary>
    internal class YooKassaOperation
    {
        /// <summary>
        /// Identifier
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Status: pending, waiting_for_capture, succeeded, canceled
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Amount
        /// </summary>
        [JsonPropertyName("amount")]
        public YooKassaAmount? Amount { get; set; }

        /// <summary>
        /// Data for the player to confirm the payment
        /// </summary>
        [JsonPropertyName("confirmation")]
        public YooKassaConfirmation? Confirmation { get; set; }

        /// <summary>
        /// Data passed at creation
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Cancellation reason
        /// </summary>
        [JsonPropertyName("cancellation_details")]
        public YooKassaCancellationDetails? CancellationDetails { get; set; }
    }
}
