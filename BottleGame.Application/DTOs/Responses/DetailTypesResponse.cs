namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response with the list of payment detail types available for a gateway
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="DetailTypesResponse"/>
    /// </remarks>
    /// <param name="gatewayCode">Gateway code</param>
    /// <param name="detailTypes">List of available detail types</param>
    public class DetailTypesResponse(string gatewayCode, List<string> detailTypes)
    {
        /// <summary>
        /// Payment gateway code
        /// </summary>
        public string GatewayCode { get; set; } = gatewayCode;

        /// <summary>
        /// List of available payment detail types (for example, "card", "phone", "sbp")
        /// </summary>
        public List<string> DetailTypes { get; set; } = detailTypes;
    }
}
