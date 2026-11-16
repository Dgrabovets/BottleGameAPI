using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Способ подтверждения платежа игроком.
    /// </summary>
    internal class YooKassaConfirmation
    {
        /// <summary>
        /// Тип подтверждения; используется redirect.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "redirect";

        /// <summary>
        /// Адрес возврата после оплаты (при создании платежа).
        /// </summary>
        [JsonPropertyName("return_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Адрес страницы оплаты (в ответе).
        /// </summary>
        [JsonPropertyName("confirmation_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ConfirmationUrl { get; set; }
    }
}
