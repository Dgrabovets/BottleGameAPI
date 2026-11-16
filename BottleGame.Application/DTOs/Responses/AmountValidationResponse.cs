namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ о валидации суммы для платёжного шлюза.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="AmountValidationResponse"/>.
    /// </remarks>
    /// <param name="gatewayCode">Код шлюза.</param>
    /// <param name="amount">Проверяемая сумма.</param>
    /// <param name="isValid">Допустима ли сумма.</param>
    public class AmountValidationResponse(string gatewayCode, decimal amount, bool isValid)
    {
        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        public string GatewayCode { get; set; } = gatewayCode;

        /// <summary>
        /// Проверяемая сумма.
        /// </summary>
        public decimal Amount { get; set; } = amount;

        /// <summary>
        /// Допустима ли сумма для данного шлюза.
        /// </summary>
        public bool IsValid { get; set; } = isValid;
    }
}
