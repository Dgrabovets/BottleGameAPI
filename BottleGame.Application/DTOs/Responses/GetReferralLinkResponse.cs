namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ с реферальной ссылкой
    /// </summary>
    public class GetReferralLinkResponse
    {
        /// <summary>
        /// Реферальная ссылка
        /// </summary>
        public string ReferralLink { get; set; } = string.Empty;
    }
}
