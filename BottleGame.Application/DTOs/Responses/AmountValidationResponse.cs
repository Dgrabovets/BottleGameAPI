namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response about the validation of an amount for a payment gateway
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="AmountValidationResponse"/>
    /// </remarks>
    /// <param name="gatewayCode">Gateway code</param>
    /// <param name="amount">Amount that was checked</param>
    /// <param name="isValid">Whether the amount is valid</param>
    public class AmountValidationResponse(string gatewayCode, decimal amount, bool isValid)
    {
        /// <summary>
        /// Payment gateway code
        /// </summary>
        public string GatewayCode { get; set; } = gatewayCode;

        /// <summary>
        /// Amount that was checked
        /// </summary>
        public decimal Amount { get; set; } = amount;

        /// <summary>
        /// Whether the amount is valid for this gateway
        /// </summary>
        public bool IsValid { get; set; } = isValid;
    }
}
