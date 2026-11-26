namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response with the exchange rate of a currency
    /// </summary>
    public class ExchangeRateResponse
    {
        /// <summary>
        /// Currency code
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Exchange rate
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ExchangeRateResponse"/>
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        /// <param name="rate">Exchange rate</param>
        public ExchangeRateResponse(string currencyCode, decimal rate)
        {
            CurrencyCode = currencyCode;
            Rate = rate;
        }
    }
}
