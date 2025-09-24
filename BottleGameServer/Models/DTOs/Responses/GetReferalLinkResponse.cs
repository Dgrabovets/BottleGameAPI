namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа получения реферальной ссылки
    /// </summary>
    public class GetReferalLinkResponse
    {
        /// <summary>
        /// Реферальная ссылка
        /// </summary>
        public string ReferalLink { get; set; } = string.Empty;
    }
}
