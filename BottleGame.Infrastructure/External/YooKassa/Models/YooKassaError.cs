using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// YooKassa API error
    /// </summary>
    internal class YooKassaError
    {
        /// <summary>
        /// Error code
        /// </summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>
        /// Error description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
