using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.External.Processing;
using BottleGame.Infrastructure.External.YooKassa.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BottleGame.Infrastructure.External.YooKassa
{
    /// <summary>
    /// Платёжный провайдер на базе API ЮKassa v3.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="YooKassaPaymentProvider"/>.
    /// </remarks>
    /// <param name="httpClient">HTTP-клиент с базовым адресом API.</param>
    /// <param name="options">Настройки провайдера.</param>
    /// <param name="logger">Логгер.</param>
    public class YooKassaPaymentProvider(
        HttpClient httpClient,
        IOptions<ProcessingOptions> options,
        ILogger<YooKassaPaymentProvider> logger) : IPaymentProvider
    {
        #region Fields

        /// <summary>
        /// HTTP-клиент.
        /// </summary>
        private readonly HttpClient _httpClient = httpClient;

        /// <summary>
        /// Настройки провайдера.
        /// </summary>
        private readonly ProcessingOptions _options = options.Value;

        /// <summary>
        /// Логгер.
        /// </summary>
        private readonly ILogger<YooKassaPaymentProvider> _logger = logger;

        /// <summary>
        /// Префикс для логов.
        /// </summary>
        private const string Prefix = "[YooKassa]";

        /// <summary>
        /// Код единственной поддерживаемой валюты.
        /// </summary>
        private const string RubCode = "rub";

        /// <summary>
        /// Код способа приёма платежей: игрок выбирает способ на странице ЮKassa.
        /// </summary>
        private const string PaymentGatewayCode = "yookassa";

        /// <summary>
        /// Код способа выплаты через СБП.
        /// </summary>
        private const string SbpGatewayCode = "sbp";

        /// <summary>
        /// Максимальная длина описания платежа в ЮKassa.
        /// </summary>
        private const int MaxDescriptionLength = 128;

        /// <summary>
        /// Ключ метаданных с идентификатором транзакции приложения.
        /// </summary>
        private const string TransactionIdKey = "transaction_id";

        /// <summary>
        /// Настройки JSON.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        #endregion Fields

        #region Catalog

        /// <inheritdoc/>
        public Task<IEnumerable<CurrencyInfo>> GetCurrenciesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<CurrencyInfo>>([new CurrencyInfo(RubCode, "Российский рубль", isActive: true)]);

        /// <inheritdoc/>
        public Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<PaymentGatewayInfo>>(
            [
                new PaymentGatewayInfo(
                    code: PaymentGatewayCode,
                    name: "ЮKassa",
                    currency: RubCode,
                    supportsPayments: true,
                    supportsPayouts: false,
                    minAmount: null,
                    maxAmount: null),
                new PaymentGatewayInfo(
                    code: SbpGatewayCode,
                    name: "СБП",
                    currency: RubCode,
                    supportsPayments: false,
                    supportsPayouts: true,
                    minAmount: 1m,
                    maxAmount: 500_000m,
                    detailTypes: ["phone"])
            ]);

        /// <inheritdoc/>
        public Task<decimal> GetExchangeRateAsync(string currency, CancellationToken cancellationToken = default) =>
            Task.FromResult(currency.Equals(RubCode, StringComparison.OrdinalIgnoreCase) ? 1m : 0m);

        #endregion Catalog

        #region Payments And Payouts

        /// <inheritdoc/>
        public async Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            var body = new YooKassaCreateRequest
            {
                Amount = ToYooKassaAmount(request.Amount, request.Currency),
                Description = Truncate(request.Description, MaxDescriptionLength),
                Metadata = new() { [TransactionIdKey] = request.TransactionId.ToString(CultureInfo.InvariantCulture) },
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
                return new CreatePaymentResult(false, payment?.Id, null, "ЮKassa не вернула ссылку на оплату");
            }

            return new CreatePaymentResult(true, payment.Id, url, null);
        }

        /// <inheritdoc/>
        public async Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.PayoutAgentId) || string.IsNullOrWhiteSpace(_options.PayoutSecretKey))
            {
                return new CreatePayoutResult(false, null, "Выплаты не настроены: задайте Processing:PayoutAgentId и Processing:PayoutSecretKey");
            }

            if (!request.Gateway.Equals(SbpGatewayCode, StringComparison.OrdinalIgnoreCase))
            {
                return new CreatePayoutResult(false, null, $"Способ выплаты '{request.Gateway}' не поддерживается");
            }

            if (!TryParseSbpDestination(request.Destination, out var destination))
            {
                return new CreatePayoutResult(false, null, "Реквизиты для СБП должны быть в формате «телефон:идентификатор банка»");
            }

            var body = new YooKassaCreateRequest
            {
                Amount = ToYooKassaAmount(request.Amount, request.Currency),
                Description = Truncate(request.Description, MaxDescriptionLength),
                Metadata = new() { [TransactionIdKey] = request.TransactionId.ToString(CultureInfo.InvariantCulture) },
                PayoutDestination = destination
            };

            var (status, json) = await SendAsync(
                HttpMethod.Post, "payouts", _options.PayoutAgentId, _options.PayoutSecretKey, body, $"payout-{request.TransactionId}", cancellationToken);

            if (!IsSuccess(status))
            {
                return new CreatePayoutResult(false, null, ReadError(json));
            }

            var payout = JsonSerializer.Deserialize<YooKassaOperation>(json, JsonOptions);
            return payout is null || string.IsNullOrEmpty(payout.Id)
                ? new CreatePayoutResult(false, null, "ЮKassa вернула пустой ответ")
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
        /// Запрашивает платёж или выплату по идентификатору.
        /// </summary>
        /// <param name="collection">Раздел API: payments или payouts.</param>
        /// <param name="id">Идентификатор у провайдера.</param>
        /// <param name="login">Идентификатор магазина или шлюза.</param>
        /// <param name="secret">Секретный ключ.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Операция или null, если провайдер её не знает.</returns>
        /// <exception cref="HttpRequestException">Провайдер ответил ошибкой, отличной от «не найдено».</exception>
        private async Task<ProviderOperation?> GetOperationAsync(string collection, string id, string login, string secret, CancellationToken cancellationToken)
        {
            var (status, json) = await SendAsync(HttpMethod.Get, $"{collection}/{Uri.EscapeDataString(id)}", login, secret, null, null, cancellationToken);

            if (status is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("{Prefix} Объект {Collection}/{Id} не найден: {Error}", Prefix, collection, id, ReadError(json));
                return null;
            }

            if (!IsSuccess(status))
            {
                throw new HttpRequestException($"ЮKassa ответила {(int)status}: {ReadError(json)}");
            }

            var operation = JsonSerializer.Deserialize<YooKassaOperation>(json, JsonOptions);
            return operation is null ? null : ToProviderOperation(operation);
        }

        /// <summary>
        /// Преобразует ответ ЮKassa в операцию провайдера.
        /// </summary>
        /// <param name="operation">Платёж или выплата в формате ЮKassa.</param>
        /// <returns>Операция провайдера.</returns>
        private static ProviderOperation ToProviderOperation(YooKassaOperation operation)
        {
            var status = operation.Status switch
            {
                "succeeded" => ProviderOperationStatus.Succeeded,
                "canceled" => ProviderOperationStatus.Canceled,
                _ => ProviderOperationStatus.Pending
            };

            int? transactionId = operation.Metadata is not null
                && operation.Metadata.TryGetValue(TransactionIdKey, out var raw)
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
        /// Отправляет запрос в ЮKassa с Basic-авторизацией.
        /// </summary>
        /// <param name="method">HTTP-метод.</param>
        /// <param name="path">Путь относительно базового адреса.</param>
        /// <param name="login">Идентификатор магазина или шлюза.</param>
        /// <param name="secret">Секретный ключ.</param>
        /// <param name="body">Тело запроса или null.</param>
        /// <param name="idempotenceKey">Ключ идемпотентности для POST.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Код ответа и тело ответа.</returns>
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
                _logger.LogWarning("{Prefix} {Method} {Path} -> {Status}: {Body}", Prefix, method, path, (int)response.StatusCode, json);
            }

            return (response.StatusCode, json);
        }

        /// <summary>
        /// Проверяет, что код ответа успешный.
        /// </summary>
        /// <param name="status">Код ответа.</param>
        /// <returns>true для кодов 2xx.</returns>
        private static bool IsSuccess(HttpStatusCode status) => (int)status is >= 200 and < 300;

        /// <summary>
        /// Достаёт описание ошибки из тела ответа.
        /// </summary>
        /// <param name="json">Тело ответа.</param>
        /// <returns>Описание ошибки.</returns>
        private static string ReadError(string json)
        {
            try
            {
                var error = JsonSerializer.Deserialize<YooKassaError>(json, JsonOptions);
                return error?.Description ?? error?.Code ?? "Неизвестная ошибка ЮKassa";
            }
            catch (JsonException)
            {
                return "Неизвестная ошибка ЮKassa";
            }
        }

        #endregion HTTP

        #region Helpers

        /// <summary>
        /// Формирует сумму в формате ЮKassa.
        /// </summary>
        /// <param name="amount">Сумма.</param>
        /// <param name="currency">Код валюты.</param>
        /// <returns>Сумма с копейками и валютой в верхнем регистре.</returns>
        private static YooKassaAmount ToYooKassaAmount(decimal amount, string currency) => new()
        {
            Value = amount.ToString("0.00", CultureInfo.InvariantCulture),
            Currency = currency.ToUpperInvariant()
        };

        /// <summary>
        /// Обрезает строку до допустимой длины.
        /// </summary>
        /// <param name="value">Строка.</param>
        /// <param name="maxLength">Максимальная длина.</param>
        /// <returns>Строка не длиннее maxLength.</returns>
        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];

        /// <summary>
        /// Разбирает реквизиты СБП в формате «телефон:идентификатор банка».
        /// </summary>
        /// <param name="destination">Реквизиты из настроек игрока.</param>
        /// <param name="result">Получатель выплаты.</param>
        /// <returns>true, если формат верный.</returns>
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
