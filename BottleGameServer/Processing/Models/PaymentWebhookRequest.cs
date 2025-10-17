namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Модель запроса, содержащий информацию о пополнении, webhook от NicePay
    /// </summary>
    [Obsolete]
    public class PaymentWebhookRequest
    {
        /// <summary>
        /// Финальный статус
        /// </summary>
        public string result { get; set; } = string.Empty;

        /// <summary>
        /// ID платежа в системе NicePay
        /// </summary>
        public string payment_id { get; set; } = string.Empty;

        /// <summary>
        /// ID мерчанта в системе NicePay
        /// </summary>
        public string merchant_id { get; set; } = string.Empty;

        /// <summary>
        /// ID платежа или клиента в вашей системе
        /// </summary>
        public string order_id { get; set; } = string.Empty;

        /// <summary>
        /// Сумма платежа (в центах/копейках)
        /// </summary>
        public int amount { get; set; }

        /// <summary>
        /// Валюта платежа
        /// </summary>
        public string amount_currency { get; set; } = string.Empty;

        /// <summary>
        /// Сумма Вашего дохода в центах/копейках. Пример: 125.28 USD это 12528
        /// </summary>
        public int profit { get; set; }

        /// <summary>
        /// Валюта дохода
        /// </summary>
        public string profit_currency { get; set; } = string.Empty;

        /// <summary>
        /// Метод оплаты
        /// </summary>
        public string method { get; set; } = string.Empty;

        /// <summary>
        /// Подпись (hash) для проверки подлинности
        /// </summary>
        public string hash { get; set; } = string.Empty;
    }
}