using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Amount in the YooKassa format: a string with kopecks and the currency code in upper case
    /// </summary>
    internal class YooKassaAmount
    {
        /// <summary>
        /// Amount, for example "100.00"
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Currency code, for example "RUB"
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
    }
}
