namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response about the validation of a payment detail type for a gateway
    /// </summary>
    public class DetailTypeValidationResponse
    {
        /// <summary>
        /// Payment gateway code
        /// </summary>
        public string GatewayCode { get; set; }

        /// <summary>
        /// Payment detail type that was checked
        /// </summary>
        public string DetailType { get; set; }

        /// <summary>
        /// Whether the gateway supports the detail type
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="DetailTypeValidationResponse"/>
        /// </summary>
        /// <param name="gatewayCode">Gateway code</param>
        /// <param name="detailType">Detail type that was checked</param>
        /// <param name="isValid">Whether the type is supported</param>
        public DetailTypeValidationResponse(string gatewayCode, string detailType, bool isValid)
        {
            GatewayCode = gatewayCode;
            DetailType = detailType;
            IsValid = isValid;
        }
    }
}
