namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Модель webhook-запроса о выплате от NicePay (успешная и неуспешная)
    /// </summary>
    [Obsolete]
    public class PayoutWebhookRequest
    {
        /// <summary>
        /// Финальный статус: success_payout, error_payout
        /// </summary>
        public string result { get; set; } = string.Empty;

        /// <summary>
        /// ID выплаты в NicePay
        /// </summary>
        public string payout_id { get; set; } = string.Empty;

        /// <summary>
        /// ID мерчанта
        /// </summary>
        public string merchant_id { get; set; } = string.Empty;

        /// <summary>
        /// ID выплаты в вашей системе
        /// </summary>
        public string order_id { get; set; } = string.Empty;

        /// <summary>
        /// Сумма выплаты (в центах/копейках)
        /// </summary>
        public int amount { get; set; }

        /// <summary>
        /// Валюта выплаты
        /// </summary>
        public string amount_currency { get; set; } = string.Empty;

        /// <summary>
        /// Метод выплаты
        /// </summary>
        public string method { get; set; } = string.Empty;

        /// <summary>
        /// Цифровая подпись (hash)
        /// </summary>
        public string hash { get; set; } = string.Empty;

        /// <summary>
        /// Причина ошибки (только для error_payout)
        /// </summary>
        public string? reason { get; set; }
    }
}