using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.External.Processing;
using BottleGame.Infrastructure.External.YooKassa.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BottleGame.Infrastructure.External.YooKassa
{
    /// <summary>
    /// Payment provider based on the YooKassa API v3
    /// </summary>
    [LogPrefix("YooKassa")]
    public class YooKassaPaymentProvider : IPaymentProvider
    {
        #region Fields

        /// <summary>
        /// HTTP client
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Provider settings
        /// </summary>
        private readonly ProcessingOptions _options;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<YooKassaPaymentProvider> _logger;

        /// <summary>
        /// Code of the only supported currency
        /// </summary>
        private const string RUB_CODE = "rub";

        /// <summary>
        /// Code of the way of accepting payments: the player chooses the method on the YooKassa page
        /// </summary>
        private const string PAYMENT_GATEWAY_CODE = "yookassa";

        /// <summary>
        /// Code of the payout method through SBP
        /// </summary>
        private const string SBP_GATEWAY_CODE = "sbp";

        /// <summary>
        /// Maximum length of a payment description in YooKassa
        /// </summary>
        private const int MAX_DESCRIPTION_LENGTH = 128;

        /// <summary>
        /// Metadata key with the application transaction identifier
        /// </summary>
        private const string TRANSACTION_ID_KEY = "transaction_id";

        /// <summary>
        /// JSON settings
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="YooKassaPaymentProvider"/>
        /// </summary>
        /// <param name="httpClient">HTTP client with the API base address</param>
        /// <param name="options">Provider settings</param>
        /// <param name="logger">Logger</param>
        public YooKassaPaymentProvider(HttpClient httpClient, IOptions<ProcessingOptions> options, ILogger<YooKassaPaymentProvider> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        #region Catalog

        /// <inheritdoc/>
        public Task<IEnumerable<CurrencyInfo>> GetCurrenciesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<CurrencyInfo>>([new CurrencyInfo(RUB_CODE, "Russian ruble", isActive: true)]);

        /// <inheritdoc/>
        public Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<PaymentGatewayInfo>>(
            [
                new PaymentGatewayInfo(
                    code: PAYMENT_GATEWAY_CODE,
                    name: "YooKassa",
                    currency: RUB_CODE,
                    supportsPayments: true,
                    supportsPayouts: false,
                    minAmount: null,
                    maxAmount: null),
                new PaymentGatewayInfo(
                    code: SBP_GATEWAY_CODE,
                    name: "SBP",
                    currency: RUB_CODE,
                    supportsPayments: false,
                    supportsPayouts: true,
                    minAmount: 1m,
                    maxAmount: 500_000m,
                    detailTypes: ["phone"])
            ]);

        /// <inheritdoc/>
        public Task<decimal> GetExchangeRateAsync(string currency, CancellationToken cancellationToken = default) =>
            Task.FromResult(currency.Equals(RUB_CODE, StringComparison.OrdinalIgnoreCase) ? 1m : 0m);

        #endregion Catalog

        #region Payments And Payouts

        /// <inheritdoc/>
        public async Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            var body = new YooKassaCreateRequest
            {
                Amount = ToYooKassaAmount(request.Amount, request.Currency),
                Description = Truncate(request.Description, MAX_DESCRIPTION_LENGTH),
                Metadata = new() { [TRANSACTION_ID_KEY] = request.TransactionId.ToString(CultureInfo.InvariantCulture) },
                Capture = true,
                Confirmation = new YooKassaConfirmation { ReturnUrl = _options.ReturnUrl }
            };

            var (status, json) = await SendAsync(
                HttpMethod.Post, "payments", _options.ShopId, _options.SecretKey, body, $"payment-{request.TransactionId}", cancellationToken);

            if (!IsSuccess(status))
            {
                return new CreatePaymentResult(false, null, null, ReadError(json));
            }

            var payment = JsonSerializer.Deserialize<YooKassaOperation>(json, JsonOptions);
            var url = payment?.Confirmation?.ConfirmationUrl;
            if (payment is null || string.IsNullOrEmpty(url))
            {
                return new CreatePaymentResult(false, payment?.Id, null, "YooKassa did not return a payment link");
            }

            return new CreatePaymentResult(true, payment.Id, url, null);
        }

        /// <inheritdoc/>
        public async Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.PayoutAgentId) || string.IsNullOrWhiteSpace(_options.PayoutSecretKey))
            {
                return new CreatePayoutResult(false, null, "Payouts are not configured: set Processing:PayoutAgentId and Processing:PayoutSecretKey");
            }

            if (!request.Gateway.Equals(SBP_GATEWAY_CODE, StringComparison.OrdinalIgnoreCase))
            {
                return new CreatePayoutResult(false, null, $"Payout method '{request.Gateway}' is not supported");
            }

            if (!TryParseSbpDestination(request.Destination, out var destination))
            {
                return new CreatePayoutResult(false, null, "SBP details must be in the format 'phone:bank id'");
            }

            var body = new YooKassaCreateRequest
            {
                Amount = ToYooKassaAmount(request.Amount, request.Currency),
                Description = Truncate(request.Description, MAX_DESCRIPTION_LENGTH),
                Metadata = new() { [TRANSACTION_ID_KEY] = request.TransactionId.ToString(CultureInfo.InvariantCulture) },
                PayoutDestination = destination
            };

            var (status, json) = await SendAsync(
                HttpMethod.Post, "payouts", _options.PayoutAgentId, _options.PayoutSecretKey, body, $"payout-{request.TransactionId}", cancellationToken);

            // Only a 4xx answer says that no payout was made. After any other answer the payout may exist,
            // so the caller must not return the money; a repeat with the same idempotence key is safe
            if ((int)status is >= 400 and < 500)
            {
                return new CreatePayoutResult(false, null, ReadError(json));
            }

            if (!IsSuccess(status))
            {
                throw new HttpRequestException($"YooKassa responded {(int)status} to a payout, the outcome is unknown: {ReadError(json)}");
            }

            var payout = JsonSerializer.Deserialize<YooKassaOperation>(json, JsonOptions);
            return payout is null || string.IsNullOrEmpty(payout.Id)
                ? throw new HttpRequestException($"YooKassa responded {(int)status} to a payout without a payout id, the outcome is unknown")
                : new CreatePayoutResult(true, payout.Id, null);
        }

        #endregion Payments And Payouts

        #region Operation Lookup

        /// <inheritdoc/>
        public Task<ProviderOperation?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default) =>
            GetOperationAsync("payments", paymentId, _options.ShopId, _options.SecretKey, cancellationToken);

        /// <inheritdoc/>
        public Task<ProviderOperation?> GetPayoutAsync(string payoutId, CancellationToken cancellationToken = default) =>
            GetOperationAsync("payouts", payoutId, _options.PayoutAgentId, _options.PayoutSecretKey, cancellationToken);

        /// <summary>
        /// Requests a payment or payout by identifier
        /// </summary>
        /// <param name="collection">API section: payments or payouts</param>
        /// <param name="id">Identifier at the provider</param>
        /// <param name="login">Shop or gateway identifier</param>
        /// <param name="secret">Secret key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The operation or null if the provider does not know it</returns>
        /// <exception cref="HttpRequestException">The provider responded with an error other than "not found"</exception>
        private async Task<ProviderOperation?> GetOperationAsync(string collection, string id, string login, string secret, CancellationToken cancellationToken)
        {
            // The id comes from an unsigned notification, so only the characters of a real id are let into the request path
            if (!IsValidObjectId(id))
            {
                _logger.LogWarning("Refused to look up {Collection} with a malformed id", collection);
                return null;
            }

            var (status, json) = await SendAsync(HttpMethod.Get, $"{collection}/{Uri.EscapeDataString(id)}", login, secret, null, null, cancellationToken);

            if (status is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Object {Collection}/{Id} not found: {Error}", collection, id, ReadError(json));
                return null;
            }

            if (!IsSuccess(status))
            {
                throw new HttpRequestException($"YooKassa responded {(int)status}: {ReadError(json)}");
            }

            var operation = JsonSerializer.Deserialize<YooKassaOperation>(json, JsonOptions);
            return operation is null ? null : ToProviderOperation(operation);
        }

        /// <summary>
        /// Checks that an identifier has only the characters of a YooKassa payment or payout id
        /// </summary>
        /// <param name="id">Identifier to check</param>
        /// <returns>true if the identifier is safe to put into a request path</returns>
        private static bool IsValidObjectId(string? id) =>
            !string.IsNullOrEmpty(id) && id.Length <= 64 && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

        /// <summary>
        /// Converts a YooKassa response into a provider operation
        /// </summary>
        /// <param name="operation">Payment or payout in the YooKassa format</param>
        /// <returns>Provider operation</returns>
        private static ProviderOperation ToProviderOperation(YooKassaOperation operation)
        {
            var status = operation.Status switch
            {
                "succeeded" => ProviderOperationStatus.Succeeded,
                "canceled" => ProviderOperationStatus.Canceled,
                _ => ProviderOperationStatus.Pending
            };

            int? transactionId = operation.Metadata is not null
                && operation.Metadata.TryGetValue(TRANSACTION_ID_KEY, out var raw)
                && int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : null;

            var reason = operation.CancellationDetails is { } details
                           ? $"{details.Party}/{details.Reason}"
                           : null;

            var amount = decimal.TryParse(operation.Amount?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0m;

            return new ProviderOperation(operation.Id, status, amount, operation.Amount?.Currency?.ToLowerInvariant() ?? string.Empty, transactionId, reason);
        }

        #endregion Operation Lookup

        #region HTTP

        /// <summary>
        /// Sends a request to YooKassa with Basic authorization
        /// </summary>
        /// <param name="method">HTTP method</param>
        /// <param name="path">Path relative to the base address</param>
        /// <param name="login">Shop or gateway identifier</param>
        /// <param name="secret">Secret key</param>
        /// <param name="body">Request body or null</param>
        /// <param name="idempotenceKey">Idempotence key for POST</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response code and response body</returns>
        private async Task<(HttpStatusCode Status, string Body)> SendAsync(
            HttpMethod method,
            string path,
            string login,
            string secret,
            object? body,
            string? idempotenceKey,
            CancellationToken cancellationToken)
        {
            using var message = new HttpRequestMessage(method, path);
            message.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}:{secret}")));

            if (idempotenceKey is not null)
            {
                message.Headers.Add("Idempotence-Key", idempotenceKey);
            }

            if (body is not null)
            {
                message.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
            }

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("{Method} {Path} -> {Status}: {Body}", method, path, (int)response.StatusCode, json);
            }

            return (response.StatusCode, json);
        }

        /// <summary>
        /// Checks that the response code is successful
        /// </summary>
        /// <param name="status">Response code</param>
        /// <returns>true for 2xx codes</returns>
        private static bool IsSuccess(HttpStatusCode status) => (int)status is >= 200 and < 300;

        /// <summary>
        /// Extracts the error description from the response body
        /// </summary>
        /// <param name="json">Response body</param>
        /// <returns>Error description</returns>
        private static string ReadError(string json)
        {
            try
            {
                var error = JsonSerializer.Deserialize<YooKassaError>(json, JsonOptions);
                return error?.Description ?? error?.Code ?? "Unknown YooKassa error";
            }
            catch (JsonException)
            {
                return "Unknown YooKassa error";
            }
        }

        #endregion HTTP

        #region Helpers

        /// <summary>
        /// Builds the amount in the YooKassa format
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <param name="currency">Currency code</param>
        /// <returns>Amount with kopecks and the currency in upper case</returns>
        private static YooKassaAmount ToYooKassaAmount(decimal amount, string currency) => new()
        {
            Value = amount.ToString("0.00", CultureInfo.InvariantCulture),
            Currency = currency.ToUpperInvariant()
        };

        /// <summary>
        /// Truncates the string to the allowed length
        /// </summary>
        /// <param name="value">String</param>
        /// <param name="maxLength">Maximum length</param>
        /// <returns>String no longer than maxLength</returns>
        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];

        /// <summary>
        /// Parses SBP details in the format "phone:bank identifier"
        /// </summary>
        /// <param name="destination">Details from the player settings</param>
        /// <param name="result">Payout recipient</param>
        /// <returns>true if the format is correct</returns>
        private static bool TryParseSbpDestination(string destination, out YooKassaPayoutDestination result)
        {
            result = new YooKassaPayoutDestination();

            var parts = destination.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[1].Length == 0)
            {
                return false;
            }

            var phone = new string(parts[0].Where(char.IsDigit).ToArray());
            if (phone.Length < 10)
            {
                return false;
            }

            result.Phone = phone;
            result.BankId = parts[1];
            return true;
        }

        #endregion Helpers
    }
}
