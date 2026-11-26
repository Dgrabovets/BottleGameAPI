namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response about the activity status of a currency
    /// </summary>
    public class CurrencyActiveResponse
    {
        /// <summary>
        /// Currency code
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Whether the currency is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="CurrencyActiveResponse"/>
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="isActive">Whether the currency is active</param>
        public CurrencyActiveResponse(string currencyCode, bool isActive)
        {
            CurrencyCode = currencyCode;
            IsActive = isActive;
        }
    }
}
