using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Controller for the catalogs of currencies and payment gateways
    /// </summary>
    /// <remarks>
    /// Gives access to the data of the payment provider:
    /// currencies, payment gateways, exchange rates.
    /// For the local enums (CurrencyType, PaymentMethodType) use GameParamsController
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of <see cref="CurrencyController"/>
    /// </remarks>
    /// <param name="currencyService">Currency service</param>
    /// <param name="logger">Logger</param>
    [ApiController]
    [Route("api/currencies")]
    [Tags("Currencies and gateways")]
    public class CurrencyController(ICurrencyService currencyService, ILogger<CurrencyController> logger) : ControllerBase
    {
        #region Fields

        /// <summary>
        /// Currency service
        /// </summary>
        private readonly ICurrencyService _currencyService = currencyService;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<CurrencyController> _logger = logger;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[Currency]";

        #endregion Fields

        #region Endpoints

        /// <summary>
        /// Gets the list of available currencies from the payment provider
        /// </summary>
        /// <param name="forceRefresh">Forces a cache refresh</param>
        /// <returns>List of currencies of the payment provider</returns>
        /// <response code="200">List of currencies</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CurrencyInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrencies([FromQuery] bool forceRefresh = false)
        {
            _logger.LogDebug("{Prefix} Currency list requested, forceRefresh={ForceRefresh}", Prefix, forceRefresh);
            var currencies = await _currencyService.GetAvailableCurrenciesAsync(forceRefresh);
            return Ok(currencies);
        }

        /// <summary>
        /// Gets the list of available payment gateways from the payment provider
        /// </summary>
        /// <param name="forceRefresh">Forces a cache refresh</param>
        /// <returns>List of payment gateways</returns>
        /// <response code="200">List of payment gateways</response>
        [HttpGet("gateways")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PaymentGatewayInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentGateways([FromQuery] bool forceRefresh = false)
        {
            _logger.LogDebug("{Prefix} Gateway list requested, forceRefresh={ForceRefresh}", Prefix, forceRefresh);
            var gateways = await _currencyService.GetPaymentGatewaysAsync(forceRefresh);
            return Ok(gateways);
        }

        /// <summary>
        /// Gets the payment gateways that accept payments in the given currency
        /// </summary>
        /// <param name="currencyCode">Currency code (for example, "rub")</param>
        /// <returns>List of payment gateways that accept payments</returns>
        /// <response code="200">List of payment gateways</response>
        [HttpGet("{currencyCode}/payment-gateways")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PaymentGatewayInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentGatewaysForCurrency(string currencyCode)
        {
            _logger.LogDebug("{Prefix} Payment gateways requested for currency {Currency}", Prefix, currencyCode);
            var gateways = await _currencyService.GetPaymentGatewaysForCurrencyAsync(currencyCode);
            return Ok(gateways);
        }

        /// <summary>
        /// Gets the payment gateways that make payouts in the given currency
        /// </summary>
        /// <param name="currencyCode">Currency code (for example, "rub")</param>
        /// <returns>List of payment gateways for payouts</returns>
        /// <response code="200">List of payment gateways</response>
        [HttpGet("{currencyCode}/payout-gateways")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PaymentGatewayInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPayoutGatewaysForCurrency(string currencyCode)
        {
            _logger.LogDebug("{Prefix} Payout gateways requested for currency {Currency}", Prefix, currencyCode);
            var gateways = await _currencyService.GetPayoutGatewaysForCurrencyAsync(currencyCode);
            return Ok(gateways);
        }

        /// <summary>
        /// Gets the current exchange rate of the given currency
        /// </summary>
        /// <param name="currencyCode">Currency code (for example, "rub")</param>
        /// <returns>Exchange rate</returns>
        /// <response code="200">Exchange rate of the currency</response>
        [HttpGet("{currencyCode}/exchange-rate")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExchangeRate(string currencyCode)
        {
            _logger.LogDebug("{Prefix} Exchange rate requested for currency {Currency}", Prefix, currencyCode);
            var rate = await _currencyService.GetExchangeRateAsync(currencyCode);
            return Ok(new ExchangeRateResponse(currencyCode, rate));
        }

        /// <summary>
        /// Checks whether the given currency is active at the payment provider
        /// </summary>
        /// <param name="currencyCode">Currency code</param>
        /// <returns>Result of the activity check</returns>
        /// <response code="200">Activity status of the currency</response>
        [HttpGet("{currencyCode}/is-active")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CurrencyActiveResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> IsCurrencyActive(string currencyCode)
        {
            _logger.LogDebug("{Prefix} Activity check for currency {Currency}", Prefix, currencyCode);
            var isActive = await _currencyService.IsCurrencyActiveAsync(currencyCode);
            return Ok(new CurrencyActiveResponse(currencyCode, isActive));
        }

        /// <summary>
        /// Checks whether the amount is valid for the given payment gateway
        /// </summary>
        /// <param name="gatewayCode">Payment gateway code</param>
        /// <param name="amount">Amount to check</param>
        /// <returns>Result of the amount validation</returns>
        /// <response code="200">Result of the amount check</response>
        [HttpGet("gateways/{gatewayCode}/validate-amount")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AmountValidationResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateAmountForGateway(string gatewayCode, [FromQuery] decimal amount)
        {
            _logger.LogDebug("{Prefix} Validation of amount {Amount} for gateway {Gateway}", Prefix, amount, gatewayCode);
            var isValid = await _currencyService.IsAmountValidForGatewayAsync(gatewayCode, amount);
            return Ok(new AmountValidationResponse(gatewayCode, amount, isValid));
        }

        /// <summary>
        /// Gets the available payment detail types of the given gateway
        /// </summary>
        /// <param name="gatewayCode">Payment gateway code</param>
        /// <returns>List of available detail types</returns>
        /// <response code="200">List of detail types (for example, "card", "phone", "sbp")</response>
        [HttpGet("gateways/{gatewayCode}/detail-types")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(DetailTypesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDetailTypesForGateway(string gatewayCode)
        {
            _logger.LogDebug("{Prefix} Detail types requested for gateway {Gateway}", Prefix, gatewayCode);
            var detailTypes = await _currencyService.GetDetailTypesForGatewayAsync(gatewayCode);
            return Ok(new DetailTypesResponse(gatewayCode, detailTypes.ToList()));
        }

        /// <summary>
        /// Checks whether the gateway supports the given type of payment details
        /// </summary>
        /// <param name="gatewayCode">Payment gateway code</param>
        /// <param name="detailType">Detail type to check</param>
        /// <returns>Result of the check</returns>
        /// <response code="200">Result of the detail type check</response>
        [HttpGet("gateways/{gatewayCode}/validate-detail-type")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(DetailTypeValidationResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateDetailTypeForGateway(string gatewayCode, [FromQuery] string detailType)
        {
            _logger.LogDebug("{Prefix} Validation of detail type '{DetailType}' for gateway {Gateway}", Prefix, detailType, gatewayCode);
            var isValid = await _currencyService.IsDetailTypeValidForGatewayAsync(gatewayCode, detailType);
            return Ok(new DetailTypeValidationResponse(gatewayCode, detailType, isValid));
        }

        /// <summary>
        /// Clears the cache of the currency and gateway catalogs
        /// </summary>
        /// <returns>Operation result</returns>
        /// <response code="200">The cache was cleared</response>
        /// <response code="401">Not authorized</response>
        /// <response code="403">Access denied</response>
        [HttpPost("cache/clear")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult ClearCache()
        {
            _currencyService.ClearCache();
            _logger.LogInformation("{Prefix} Catalog cache cleared", Prefix);
            return Ok(new { message = "Cache cleared" });
        }

        #endregion Endpoints
    }
}
