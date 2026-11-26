namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response about the validation of an amount for a payment gateway
    /// </summary>
    public class AmountValidationResponse
    {
        /// <summary>
        /// Payment gateway code
        /// </summary>
        public string GatewayCode { get; set; }

        /// <summary>
        /// Amount that was checked
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Whether the amount is valid for this gateway
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="AmountValidationResponse"/>
        /// </summary>
        /// <param name="gatewayCode">Gateway code</param>
        /// <param name="amount">Amount that was checked</param>
        /// <param name="isValid">Whether the amount is valid</param>
        public AmountValidationResponse(string gatewayCode, decimal amount, bool isValid)
        {
            GatewayCode = gatewayCode;
            Amount = amount;
            IsValid = isValid;
        }
    }
}
