namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ со списком доступных типов платёжных реквизитов для шлюза.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="DetailTypesResponse"/>.
    /// </remarks>
    /// <param name="gatewayCode">Код шлюза.</param>
    /// <param name="detailTypes">Список доступных типов реквизитов.</param>
    public class DetailTypesResponse(string gatewayCode, List<string> detailTypes)
    {
        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        public string GatewayCode { get; set; } = gatewayCode;

        /// <summary>
        /// Список доступных типов платёжных реквизитов (например, "card", "phone", "sbp").
        /// </summary>
        public List<string> DetailTypes { get; set; } = detailTypes;
    }
}
