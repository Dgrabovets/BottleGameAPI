namespace BottleGame.Infrastructure.External.Processing
{
    /// <summary>
    /// Payment provider settings (YooKassa)
    /// </summary>
    public class ProcessingOptions
    {
        /// <summary>
        /// Mode of working with the payment provider. Set through Processing__Mode
        /// </summary>
        /// <remarks>Default: Simulated. Set Live for real payments</remarks>
        public ProcessingMode Mode { get; set; } = ProcessingMode.Simulated;

        /// <summary>
        /// Base URL of the YooKassa API
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.yookassa.ru/v3/";

        /// <summary>
        /// Shop identifier for accepting payments. Set through Processing__ShopId
        /// </summary>
        public string ShopId { get; set; } = string.Empty;

        /// <summary>
        /// Secret key of the shop. Set through Processing__SecretKey
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Payout gateway identifier. Set through Processing__PayoutAgentId; payouts are unavailable without it
        /// </summary>
        public string PayoutAgentId { get; set; } = string.Empty;

        /// <summary>
        /// Secret key of the payout gateway. Set through Processing__PayoutSecretKey
        /// </summary>
        public string PayoutSecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Address of the page YooKassa returns the player to after payment. Set through Processing__ReturnUrl
        /// </summary>
        public string ReturnUrl { get; set; } = string.Empty;

        /// <summary>
        /// Maximum time to wait for an API response, in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
