using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Recipient of an SBP payout
    /// </summary>
    internal class YooKassaPayoutDestination
    {
        /// <summary>
        /// Recipient type; sbp is used
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "sbp";

        /// <summary>
        /// Recipient phone in the format 79000000000
        /// </summary>
        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Bank identifier from the SBP directory
        /// </summary>
        [JsonPropertyName("bank_id")]
        public string BankId { get; set; } = string.Empty;
    }
}
