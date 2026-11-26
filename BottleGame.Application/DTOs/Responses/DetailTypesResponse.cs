namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response with the list of payment detail types available for a gateway
    /// </summary>
    public class DetailTypesResponse
    {
        /// <summary>
        /// Payment gateway code
        /// </summary>
        public string GatewayCode { get; set; }

        /// <summary>
        /// List of available payment detail types (for example, "card", "phone", "sbp")
        /// </summary>
        public List<string> DetailTypes { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="DetailTypesResponse"/>
        /// </summary>
        /// <param name="gatewayCode">Gateway code</param>
        /// <param name="detailTypes">List of available detail types</param>
        public DetailTypesResponse(string gatewayCode, List<string> detailTypes)
        {
            GatewayCode = gatewayCode;
            DetailTypes = detailTypes;
        }
    }
}
