using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Ошибка API ЮKassa.
    /// </summary>
    internal class YooKassaError
    {
        /// <summary>
        /// Код ошибки.
        /// </summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>
        /// Описание ошибки.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
