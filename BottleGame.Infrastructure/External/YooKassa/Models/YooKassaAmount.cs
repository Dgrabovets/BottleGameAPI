using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Сумма в формате ЮKassa: строка с копейками и код валюты в верхнем регистре.
    /// </summary>
    internal class YooKassaAmount
    {
        /// <summary>
        /// Сумма, например "100.00".
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Код валюты, например "RUB".
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
    }
}
