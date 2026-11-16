namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ с курсом обмена валюты.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="ExchangeRateResponse"/>.
    /// </remarks>
    /// <param name="currencyCode">Код валюты.</param>
    /// <param name="rate">Курс обмена.</param>
    public class ExchangeRateResponse(string currencyCode, decimal rate)
    {
        /// <summary>
        /// Код валюты.
        /// </summary>
        public string CurrencyCode { get; set; } = currencyCode;

        /// <summary>
        /// Курс обмена.
        /// </summary>
        public decimal Rate { get; set; } = rate;
    }
}
