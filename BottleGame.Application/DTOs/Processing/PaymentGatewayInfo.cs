namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Payment gateway information
    /// </summary>
    public class PaymentGatewayInfo
    {
        /// <summary>
        /// Gateway code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gateway name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Currency code
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Whether payments are supported
        /// </summary>
        public bool SupportsPayments { get; set; }

        /// <summary>
        /// Whether payouts are supported
        /// </summary>
        public bool SupportsPayouts { get; set; }

        /// <summary>
        /// Minimum amount
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Maximum amount
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Types of payment details available for this gateway (for example, "card", "phone", "account_number")
        /// </summary>
        public List<string> DetailTypes { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of <see cref="PaymentGatewayInfo"/>
        /// </summary>
        public PaymentGatewayInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PaymentGatewayInfo"/>
        /// </summary>
        /// <param name="code">Gateway code</param>
        /// <param name="name">Gateway name</param>
        /// <param name="currency">Currency code</param>
        /// <param name="supportsPayments">Whether payments are supported</param>
        /// <param name="supportsPayouts">Whether payouts are supported</param>
        /// <param name="minAmount">Minimum amount</param>
        /// <param name="maxAmount">Maximum amount</param>
        /// <param name="detailTypes">Types of payment details</param>
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
