namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Унифицированная модель webhook от NicePay (платежи и выплаты)
    /// </summary>
    public class NicePayWebhookRequest
    {
        /// <summary>
        /// Финальный статус
        /// </summary>
        public string result { get; set; } = string.Empty;

        /// <summary>
        /// ID платежа в системе NicePay (для платежей)
        /// </summary>
        public string? payment_id { get; set; }

        /// <summary>
        /// ID выплаты в системе NicePay (для выплат)
        /// </summary>
        public string? payout_id { get; set; }

        /// <summary>
        /// ID мерчанта в системе NicePay
        /// </summary>
        public string merchant_id { get; set; } = string.Empty;

        /// <summary>
        /// ID платежа или клиента в вашей системе
        /// </summary>
        public string order_id { get; set; } = string.Empty;

        /// <summary>
        /// Сумма платежа/выплаты (в центах/копейках)
        /// </summary>
        public int? amount { get; set; }

        /// <summary>
        /// Валюта платежа/выплаты
        /// </summary>
        public string? amount_currency { get; set; }

        /// <summary>
        /// Сумма Вашего дохода в центах/копейках (только для платежей)
        /// </summary>
        public int? profit { get; set; }

        /// <summary>
        /// Валюта дохода (только для платежей)
        /// </summary>
        public string? profit_currency { get; set; }

        /// <summary>
        /// Метод оплаты/выплаты
        /// </summary>
        public string? method { get; set; }

        /// <summary>
        /// Причина ошибки (только для выплат с ошибкой)
        /// </summary>
        public string? reason { get; set; }

        /// <summary>
        /// Подпись (hash) для проверки подлинности
        /// </summary>
        public string hash { get; set; } = string.Empty;

        /// <summary>
        /// Определяет тип webhook (платеж или выплата)
        /// </summary>
        public WebhookType Type
        {
            get
            {
                if (result == "success" || result == "error")
                    return WebhookType.Payment;
                if (result == "success_payout" || result == "error_payout")
                    return WebhookType.Payout;
                return WebhookType.Unknown;
            }
        }
    }

    public enum WebhookType
    {
        Unknown,
        Payment,
        Payout
    }
}