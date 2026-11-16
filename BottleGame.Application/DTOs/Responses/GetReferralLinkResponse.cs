namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response with a referral link
    /// </summary>
    public class GetReferralLinkResponse
    {
        /// <summary>
        /// Referral link
        /// </summary>
        public string ReferralLink { get; set; } = string.Empty;
    }
}
