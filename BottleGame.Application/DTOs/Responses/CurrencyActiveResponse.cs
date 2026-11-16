namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response about the activity status of a currency
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="CurrencyActiveResponse"/>
    /// </remarks>
    /// <param name="currencyCode">Currency code</param>
    /// <param name="isActive">Whether the currency is active</param>
    public class CurrencyActiveResponse(string currencyCode, bool isActive)
    {
        /// <summary>
        /// Currency code
        /// </summary>
        public string CurrencyCode { get; set; } = currencyCode;

        /// <summary>
        /// Whether the currency is active
        /// </summary>
        public bool IsActive { get; set; } = isActive;
    }
}
