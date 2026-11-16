namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ о валидации типа платёжных реквизитов для шлюза.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="DetailTypeValidationResponse"/>.
    /// </remarks>
    /// <param name="gatewayCode">Код шлюза.</param>
    /// <param name="detailType">Проверяемый тип реквизитов.</param>
    /// <param name="isValid">Поддерживается ли тип.</param>
    public class DetailTypeValidationResponse(string gatewayCode, string detailType, bool isValid)
    {
        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        public string GatewayCode { get; set; } = gatewayCode;

        /// <summary>
        /// Проверяемый тип платёжных реквизитов.
        /// </summary>
        public string DetailType { get; set; } = detailType;

        /// <summary>
        /// Поддерживается ли тип реквизитов данным шлюзом.
        /// </summary>
        public bool IsValid { get; set; } = isValid;
    }
}
