namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ о статусе активности валюты.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="CurrencyActiveResponse"/>.
    /// </remarks>
    /// <param name="currencyCode">Код валюты.</param>
    /// <param name="isActive">Активна ли валюта.</param>
    public class CurrencyActiveResponse(string currencyCode, bool isActive)
    {
        /// <summary>
        /// Код валюты.
        /// </summary>
        public string CurrencyCode { get; set; } = currencyCode;

        /// <summary>
        /// Активна ли валюта.
        /// </summary>
        public bool IsActive { get; set; } = isActive;
    }
}
