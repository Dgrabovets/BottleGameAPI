using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.YooKassa.Models
{
    /// <summary>
    /// Получатель СБП-выплаты.
    /// </summary>
    internal class YooKassaPayoutDestination
    {
        /// <summary>
        /// Тип получателя; используется sbp.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "sbp";

        /// <summary>
        /// Телефон получателя в формате 79000000000.
        /// </summary>
        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор банка из справочника СБП.
        /// </summary>
        [JsonPropertyName("bank_id")]
        public string BankId { get; set; } = string.Empty;
    }
}
