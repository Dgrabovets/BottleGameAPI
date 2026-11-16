namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response about the validation of a payment detail type for a gateway
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="DetailTypeValidationResponse"/>
    /// </remarks>
    /// <param name="gatewayCode">Gateway code</param>
    /// <param name="detailType">Detail type that was checked</param>
    /// <param name="isValid">Whether the type is supported</param>
    public class DetailTypeValidationResponse(string gatewayCode, string detailType, bool isValid)
    {
        /// <summary>
        /// Payment gateway code
        /// </summary>
        public string GatewayCode { get; set; } = gatewayCode;

        /// <summary>
        /// Payment detail type that was checked
        /// </summary>
        public string DetailType { get; set; } = detailType;

        /// <summary>
        /// Whether the gateway supports the detail type
        /// </summary>
        public bool IsValid { get; set; } = isValid;
    }
}
