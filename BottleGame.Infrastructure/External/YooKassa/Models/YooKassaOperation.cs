using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Платёж или выплата в ответе ЮKassa.
    /// </summary>
    internal class YooKassaOperation
    {
        /// <summary>
        /// Идентификатор.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Статус: pending, waiting_for_capture, succeeded, canceled.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Сумма.
        /// </summary>
        [JsonPropertyName("amount")]
        public YooKassaAmount? Amount { get; set; }

        /// <summary>
        /// Данные для подтверждения платежа игроком.
        /// </summary>
        [JsonPropertyName("confirmation")]
        public YooKassaConfirmation? Confirmation { get; set; }

        /// <summary>
        /// Данные, переданные при создании.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Причина отмены.
        /// </summary>
        [JsonPropertyName("cancellation_details")]
        public YooKassaCancellationDetails? CancellationDetails { get; set; }
    }
}
