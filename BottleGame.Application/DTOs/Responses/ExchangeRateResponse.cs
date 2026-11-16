namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response with the exchange rate of a currency
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="ExchangeRateResponse"/>
    /// </remarks>
    /// <param name="currencyCode">Currency code</param>
    /// <param name="rate">Exchange rate</param>
    public class ExchangeRateResponse(string currencyCode, decimal rate)
    {
        /// <summary>
        /// Currency code
        /// </summary>
        public string CurrencyCode { get; set; } = currencyCode;

        /// <summary>
        /// Exchange rate
        /// </summary>
        public decimal Rate { get; set; } = rate;
    }
}
