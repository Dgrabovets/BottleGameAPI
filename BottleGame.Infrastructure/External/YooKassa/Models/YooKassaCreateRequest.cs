using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Тело запроса на создание платежа или выплаты.
    /// </summary>
    internal class YooKassaCreateRequest
    {
        /// <summary>
        /// Сумма.
        /// </summary>
        [JsonPropertyName("amount")]
        public YooKassaAmount Amount { get; set; } = new();

        /// <summary>
        /// Описание операции.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Произвольные данные, которые ЮKassa вернёт вместе с объектом.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = [];

        /// <summary>
        /// Признак автоматического списания (только для платежа).
        /// </summary>
        [JsonPropertyName("capture")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Capture { get; set; }

        /// <summary>
        /// Подтверждение платежа игроком (только для платежа).
        /// </summary>
        [JsonPropertyName("confirmation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public YooKassaConfirmation? Confirmation { get; set; }

        /// <summary>
        /// Получатель выплаты (только для выплаты).
        /// </summary>
        [JsonPropertyName("payout_destination_data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public YooKassaPayoutDestination? PayoutDestination { get; set; }
    }
}
