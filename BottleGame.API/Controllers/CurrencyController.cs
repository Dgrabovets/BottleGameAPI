using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер для работы со справочниками валют и платёжных шлюзов из процессинга.
    /// </summary>
    /// <remarks>
    /// Предоставляет доступ к данным процессинговой системы:
    /// валюты, платёжные шлюзы, курсы обмена.
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="CurrencyController"/>.
    /// </remarks>
    /// <param name="currencyService">Сервис валют.</param>
    /// <param name="logger">Логгер.</param>
    [ApiController]
    [Route("api/currencies")]
    [Tags("Валюты и шлюзы (Processing)")]
    public class CurrencyController(ICurrencyService currencyService, ILogger<CurrencyController> logger) : ControllerBase
    {
        #region Fields

        /// <summary>
        /// Сервис валют.
        /// </summary>
        private readonly ICurrencyService _currencyService = currencyService;

        /// <summary>
        /// Логгер.
        /// </summary>
        private readonly ILogger<CurrencyController> _logger = logger;

        /// <summary>
        /// Префикс для логов.
        /// </summary>
        private const string Prefix = "[Currency]";

        #endregion Fields

        #region Endpoints

        /// <summary>
        /// Получает список доступных валют из процессинга.
        /// </summary>
        /// <param name="forceRefresh">Принудительно обновить кэш.</param>
        /// <returns>Список валют из процессинговой системы.</returns>
        /// <response code="200">Список валют.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CurrencyInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrencies([FromQuery] bool forceRefresh = false)
        {
            _logger.LogDebug("{Prefix} Запрос списка валют, forceRefresh={ForceRefresh}", Prefix, forceRefresh);
            var currencies = await _currencyService.GetAvailableCurrenciesAsync(forceRefresh);
            return Ok(currencies);
        }

        /// <summary>
        /// Получает список доступных платёжных шлюзов из процессинга.
        /// </summary>
        /// <param name="forceRefresh">Принудительно обновить кэш.</param>
        /// <returns>Список платёжных шлюзов.</returns>
        /// <response code="200">Список платёжных шлюзов.</response>
        [HttpGet("gateways")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PaymentGatewayInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentGateways([FromQuery] bool forceRefresh = false)
        {
            _logger.LogDebug("{Prefix} Запрос списка шлюзов, forceRefresh={ForceRefresh}", Prefix, forceRefresh);
            var gateways = await _currencyService.GetPaymentGatewaysAsync(forceRefresh);
            return Ok(gateways);
        }

        /// <summary>
        /// Получает платёжные шлюзы для приёма платежей в указанной валюте.
        /// </summary>
        /// <param name="currencyCode">Код валюты (например, "rub").</param>
        /// <returns>Список платёжных шлюзов для приёма платежей.</returns>
        /// <response code="200">Список платёжных шлюзов.</response>
        [HttpGet("{currencyCode}/payment-gateways")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PaymentGatewayInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentGatewaysForCurrency(string currencyCode)
        {
            _logger.LogDebug("{Prefix} Запрос шлюзов для платежей в валюте {Currency}", Prefix, currencyCode);
            var gateways = await _currencyService.GetPaymentGatewaysForCurrencyAsync(currencyCode);
            return Ok(gateways);
        }

        /// <summary>
        /// Получает платёжные шлюзы для выплат в указанной валюте.
        /// </summary>
        /// <param name="currencyCode">Код валюты (например, "rub").</param>
        /// <returns>Список платёжных шлюзов для выплат.</returns>
        /// <response code="200">Список платёжных шлюзов.</response>
        [HttpGet("{currencyCode}/payout-gateways")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PaymentGatewayInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPayoutGatewaysForCurrency(string currencyCode)
        {
            _logger.LogDebug("{Prefix} Запрос шлюзов для выплат в валюте {Currency}", Prefix, currencyCode);
            var gateways = await _currencyService.GetPayoutGatewaysForCurrencyAsync(currencyCode);
            return Ok(gateways);
        }

        /// <summary>
        /// Получает текущий курс обмена для указанной валюты.
        /// </summary>
        /// <param name="currencyCode">Код валюты (например, "rub").</param>
        /// <returns>Курс обмена.</returns>
        /// <response code="200">Курс обмена валюты.</response>
        [HttpGet("{currencyCode}/exchange-rate")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExchangeRate(string currencyCode)
        {
            _logger.LogDebug("{Prefix} Запрос курса для валюты {Currency}", Prefix, currencyCode);
            var rate = await _currencyService.GetExchangeRateAsync(currencyCode);
            return Ok(new ExchangeRateResponse(currencyCode, rate));
        }

        /// <summary>
        /// Проверяет, активна ли указанная валюта в процессинге.
        /// </summary>
        /// <param name="currencyCode">Код валюты.</param>
        /// <returns>Результат проверки активности.</returns>
        /// <response code="200">Статус активности валюты.</response>
        [HttpGet("{currencyCode}/is-active")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CurrencyActiveResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> IsCurrencyActive(string currencyCode)
        {
            _logger.LogDebug("{Prefix} Проверка активности валюты {Currency}", Prefix, currencyCode);
            var isActive = await _currencyService.IsCurrencyActiveAsync(currencyCode);
            return Ok(new CurrencyActiveResponse(currencyCode, isActive));
        }

        /// <summary>
        /// Проверяет, допустима ли сумма для указанного платёжного шлюза.
        /// </summary>
        /// <param name="gatewayCode">Код платёжного шлюза.</param>
        /// <param name="amount">Сумма для проверки.</param>
        /// <returns>Результат валидации суммы.</returns>
        /// <response code="200">Результат проверки суммы.</response>
        [HttpGet("gateways/{gatewayCode}/validate-amount")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AmountValidationResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateAmountForGateway(string gatewayCode, [FromQuery] decimal amount)
        {
            _logger.LogDebug("{Prefix} Валидация суммы {Amount} для шлюза {Gateway}", Prefix, amount, gatewayCode);
            var isValid = await _currencyService.IsAmountValidForGatewayAsync(gatewayCode, amount);
            return Ok(new AmountValidationResponse(gatewayCode, amount, isValid));
        }

        /// <summary>
        /// Получает доступные типы платёжных реквизитов для указанного шлюза.
        /// </summary>
        /// <param name="gatewayCode">Код платёжного шлюза.</param>
        /// <returns>Список доступных типов реквизитов.</returns>
        /// <response code="200">Список типов реквизитов (например, "card", "phone", "sbp").</response>
        [HttpGet("gateways/{gatewayCode}/detail-types")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(DetailTypesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDetailTypesForGateway(string gatewayCode)
        {
            _logger.LogDebug("{Prefix} Запрос типов реквизитов для шлюза {Gateway}", Prefix, gatewayCode);
            var detailTypes = await _currencyService.GetDetailTypesForGatewayAsync(gatewayCode);
            return Ok(new DetailTypesResponse(gatewayCode, detailTypes.ToList()));
        }

        /// <summary>
        /// Проверяет, поддерживает ли шлюз указанный тип платёжных реквизитов.
        /// </summary>
        /// <param name="gatewayCode">Код платёжного шлюза.</param>
        /// <param name="detailType">Тип реквизитов для проверки.</param>
        /// <returns>Результат проверки.</returns>
        /// <response code="200">Результат проверки типа реквизитов.</response>
        [HttpGet("gateways/{gatewayCode}/validate-detail-type")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(DetailTypeValidationResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateDetailTypeForGateway(string gatewayCode, [FromQuery] string detailType)
        {
            _logger.LogDebug("{Prefix} Валидация типа реквизитов '{DetailType}' для шлюза {Gateway}", Prefix, detailType, gatewayCode);
            var isValid = await _currencyService.IsDetailTypeValidForGatewayAsync(gatewayCode, detailType);
            return Ok(new DetailTypeValidationResponse(gatewayCode, detailType, isValid));
        }

        /// <summary>
        /// Очищает кэш справочников валют и шлюзов.
        /// </summary>
        /// <returns>Результат операции.</returns>
        /// <response code="200">Кэш успешно очищен.</response>
        /// <response code="401">Не авторизован.</response>
        /// <response code="403">Доступ запрещён.</response>
        [HttpPost("cache/clear")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult ClearCache()
        {
            _currencyService.ClearCache();
            _logger.LogInformation("{Prefix} Кэш справочников очищен", Prefix);
            return Ok(new { message = "Кэш очищен" });
        }

        #endregion Endpoints
    }
}
