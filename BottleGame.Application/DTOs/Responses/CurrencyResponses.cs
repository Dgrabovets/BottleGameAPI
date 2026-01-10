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

