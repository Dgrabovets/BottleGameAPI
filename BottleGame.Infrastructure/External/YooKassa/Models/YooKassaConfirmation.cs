using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Way for the player to confirm the payment
    /// </summary>
    internal class YooKassaConfirmation
    {
        /// <summary>
        /// Confirmation type; redirect is used
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "redirect";

        /// <summary>
        /// Return address after payment (when creating a payment)
        /// </summary>
        [JsonPropertyName("return_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Address of the payment page (in the response)
        /// </summary>
        [JsonPropertyName("confirmation_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ConfirmationUrl { get; set; }
    }
}
