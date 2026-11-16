namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Информация о платёжном шлюзе.
    /// </summary>
    public class PaymentGatewayInfo
    {
        /// <summary>
        /// Код шлюза.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Название шлюза.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Код валюты.
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Поддерживает ли платежи.
        /// </summary>
        public bool SupportsPayments { get; set; }

        /// <summary>
        /// Поддерживает ли выплаты.
        /// </summary>
        public bool SupportsPayouts { get; set; }

        /// <summary>
        /// Минимальная сумма.
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Максимальная сумма.
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Типы платёжных реквизитов, доступные для этого шлюза (например, "card", "phone", "account_number").
        /// </summary>
        public List<string> DetailTypes { get; set; } = [];

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PaymentGatewayInfo"/>.
        /// </summary>
        public PaymentGatewayInfo()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PaymentGatewayInfo"/>.
        /// </summary>
        /// <param name="code">Код шлюза.</param>
        /// <param name="name">Название шлюза.</param>
        /// <param name="currency">Код валюты.</param>
        /// <param name="supportsPayments">Поддерживает ли платежи.</param>
        /// <param name="supportsPayouts">Поддерживает ли выплаты.</param>
        /// <param name="minAmount">Минимальная сумма.</param>
        /// <param name="maxAmount">Максимальная сумма.</param>
        /// <param name="detailTypes">Типы платёжных реквизитов.</param>
        public PaymentGatewayInfo(string code, string name, string currency, bool supportsPayments, bool supportsPayouts, decimal? minAmount, decimal? maxAmount, List<string>? detailTypes = null)
        {
            Code = code;
            Name = name;
            Currency = currency;
            SupportsPayments = supportsPayments;
            SupportsPayouts = supportsPayouts;
            MinAmount = minAmount;
            MaxAmount = maxAmount;
            DetailTypes = detailTypes ?? [];
        }
    }
}
