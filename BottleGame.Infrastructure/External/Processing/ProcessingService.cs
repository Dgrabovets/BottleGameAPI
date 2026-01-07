using System.Net.Http.Json;
using System.Text.Json;
using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BottleGame.Infrastructure.External.Processing
{
    /// <summary>
    /// Реализация сервиса интеграции с платёжным процессингом.
    /// </summary>
    public class ProcessingService : IProcessingService
    {
        #region Fields

        /// <summary>
        /// HTTP-клиент для взаимодействия с платёжным API.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Настройки платёжного процессинга.
        /// </summary>
        private readonly ProcessingOptions _options;

        /// <summary>
        /// Логгер для записи событий сервиса.
        /// </summary>
        private readonly ILogger<ProcessingService> _logger;

        /// <summary>
        /// Префикс для логов.
        /// </summary>
        private const string Prefix = "[ProcessingService]";

        /// <summary>
        /// Настройки JSON-сериализации.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ProcessingService"/>.
        /// </summary>
        /// <param name="httpClient">HTTP-клиент для взаимодействия с платёжным API.</param>
        /// <param name="options">Настройки платёжного процессинга.</param>
        /// <param name="logger">Логгер для записи событий.</param>
        public ProcessingService(
            HttpClient httpClient,
            IOptions<ProcessingOptions> options,
            ILogger<ProcessingService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            // Настраиваем базовые заголовки согласно документации PayShark API
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Access-Token", _options.AccessToken);
            if (_options.MaxWaitMs > 0)
            {
                _httpClient.DefaultRequestHeaders.Add("X-Max-Wait-Ms", _options.MaxWaitMs.ToString());
            }
        }

        #endregion Constructor

        #region Справочники

        /// <inheritdoc/>
        public async Task<IEnumerable<CurrencyInfo>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("api/currencies", cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<List<ProcessingCurrencyResponse>>>(content, JsonOptions);
                    if (result?.Success is true && result.Data is not null)
                    {
                        return result.Data.Select(c => new CurrencyInfo(c.Currency, c.Name, true));
                    }
                }

                _logger.LogWarning("{Prefix} Ошибка получения валют: {Content}", Prefix, content);
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при получении валют", Prefix);
                return [];
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("api/payment-gateways", cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<List<ProcessingGatewayResponse>>>(content, JsonOptions);
                    if (result?.Success is true && result.Data is not null)
                    {
                        return result.Data.Select(g => new PaymentGatewayInfo(
                            g.Code,
                            g.Name,
                            g.Currency,
                            g.SupportsPayments,
                            g.SupportsPayouts,
                            g.MinAmount,
                            g.MaxAmount));
                    }
                }

                _logger.LogWarning("{Prefix} Ошибка получения платёжных шлюзов: {Content}", Prefix, content);
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при получении платёжных шлюзов", Prefix);
                return [];
            }
        }

        /// <inheritdoc/>
        public async Task<decimal> GetMerchantExchangeRateAsync(string currency, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/merchant/{_options.MerchantId}/exchange-rate?currency={currency}", cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<ProcessingExchangeRateResponse>>(content, JsonOptions);
                    if (result?.Success is true && result.Data is not null)
                    {
                        return result.Data.Rate;
                    }
                }

                _logger.LogWarning("{Prefix} Ошибка получения курса для {Currency}: {Content}", Prefix, currency, content);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при получении курса для {Currency}", Prefix, currency);
                return 0;
            }
        }

        #endregion Справочники

        #region Сделки (Orders)

        /// <inheritdoc/>
        public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var apiRequest = new ProcessingCreateOrderApiRequest
                {
                    MerchantId = request.MerchantId,
                    ExternalId = request.ExternalId,
                    Amount = request.Amount,
                    PaymentGateway = request.PaymentGateway,
                    PaymentDetailType = request.PaymentDetailType,
                    ClientId = request.ClientId,
                    CallbackUrl = request.CallbackUrl ?? $"{_options.CallbackUrl}/order-status",
                    SuccessUrl = request.SuccessUrl,
                    FailUrl = request.FailUrl
                };

                _logger.LogInformation("{Prefix} Создание ордера: ExternalId={ExternalId}, Amount={Amount}, Gateway={Gateway}",
                    Prefix, request.ExternalId, request.Amount, request.PaymentGateway);

                var response = await _httpClient.PostAsJsonAsync("api/merchant/order", apiRequest, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<ProcessingOrderApiResponse>>(content, JsonOptions);
                    if (result?.Success is true && result.Data is not null)
                    {
                        _logger.LogInformation("{Prefix} Ордер создан успешно: UUID={Uuid}", Prefix, result.Data.Uuid);
                        return new CreateOrderResult(true, result.Data.Uuid, result.Data.PaymentUrl);
                    }
                }

                var errorResult = JsonSerializer.Deserialize<ProcessingApiErrorResponse>(content, JsonOptions);
                var errorMessage = errorResult?.Message ?? "Неизвестная ошибка";

                _logger.LogWarning("{Prefix} Ошибка создания ордера: {Error}", Prefix, errorMessage);
                return new CreateOrderResult(false, null, null, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при создании ордера", Prefix);
                return new CreateOrderResult(false, null, null, ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<OrderDetails?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/order/{orderId}", cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<ProcessingOrderApiResponse>>(content, JsonOptions);
                    if (result?.Success is true && result.Data is not null)
                    {
                        return MapToOrderDetails(result.Data);
                    }
                }

                _logger.LogWarning("{Prefix} Ордер не найден: {OrderId}", Prefix, orderId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при получении ордера {OrderId}", Prefix, orderId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<OrderDetails?> GetOrderByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/order/external/{externalId}", cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<ProcessingOrderApiResponse>>(content, JsonOptions);
                    if (result?.Success is true && result.Data is not null)
                    {
                        return MapToOrderDetails(result.Data);
                    }
                }

                _logger.LogWarning("{Prefix} Ордер не найден по external_id: {ExternalId}", Prefix, externalId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при получении ордера по external_id {ExternalId}", Prefix, externalId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/order/{orderId}/cancel", null, cancellationToken);
                var success = response.IsSuccessStatusCode;

                _logger.LogInformation("{Prefix} Отмена ордера {OrderId}: {Success}", Prefix, orderId, success);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при отмене ордера {OrderId}", Prefix, orderId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ConfirmOrderAsync(string orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/order/{orderId}/confirm", null, cancellationToken);
                var success = response.IsSuccessStatusCode;

                _logger.LogInformation("{Prefix} Подтверждение ордера {OrderId}: {Success}", Prefix, orderId, success);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при подтверждении ордера {OrderId}", Prefix, orderId);
                return false;
            }
        }

        #endregion Сделки

        #region Выплаты (Payouts)

        /// <inheritdoc/>
        public async Task<CreatePayoutResult> CreateDirectPayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var apiRequest = new ProcessingDirectPayoutApiRequest
                {
                    MerchantId = request.MerchantId,
                    ExternalId = request.ExternalId,
                    Amount = request.Amount,
                    Wallet = request.Wallet,
                    Currency = request.Currency,
                    CallbackUrl = request.CallbackUrl ?? $"{_options.CallbackUrl}/payout-status"
                };

                _logger.LogInformation("{Prefix} Создание прямой выплаты: ExternalId={ExternalId}, Amount={Amount}, Gateway={Gateway}",
                    Prefix, request.ExternalId, request.Amount, request.PaymentGateway);

                // Согласно документации PayShark, прямая выплата может требовать gateway в URL или в теле запроса
                // Проверяем документацию: в примерах не показан endpoint для прямой выплаты
                // Используем endpoint с gateway в URL (может потребоваться корректировка после тестирования)
                var response = await _httpClient.PostAsJsonAsync($"api/payout/{request.PaymentGateway}/direct", apiRequest, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                return ParsePayoutResponse(content, response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при создании прямой выплаты", Prefix);
                return new CreatePayoutResult(false, null, null, ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<CreatePayoutResult> CreatePoolPayoutAsync(CreatePoolPayoutRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var apiRequest = new ProcessingPoolPayoutApiRequest
                {
                    MerchantId = request.MerchantId,
                    ExternalId = request.ExternalId,
                    Amount = request.Amount,
                    Wallet = request.Wallet,
                    Currency = request.Currency,
                    WaitingTime = request.WaitingTimeMinutes,
                    CallbackUrl = request.CallbackUrl ?? $"{_options.CallbackUrl}/payout-status"
                };

                _logger.LogInformation("{Prefix} Создание выплаты через пул: ExternalId={ExternalId}, Amount={Amount}, Gateway={Gateway}, WaitingTime={WaitingTime}",
                    Prefix, request.ExternalId, request.Amount, request.PaymentGateway, request.WaitingTimeMinutes);

                // Согласно документации PayShark: POST /api/payout/pool (без gateway в URL)
                var response = await _httpClient.PostAsJsonAsync("api/payout/pool", apiRequest, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                return ParsePayoutResponse(content, response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при создании выплаты через пул", Prefix);
                return new CreatePayoutResult(false, null, null, ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<PayoutDetails?> GetPayoutAsync(string payoutId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/payout/{payoutId}", cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<ProcessingPayoutApiResponse>>(content, JsonOptions);
                    if (result?.Success is true && result.Data is not null)
                    {
                        return MapToPayoutDetails(result.Data);
                    }
                }

                _logger.LogWarning("{Prefix} Выплата не найдена: {PayoutId}", Prefix, payoutId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при получении выплаты {PayoutId}", Prefix, payoutId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<DisputeResult> OpenPayoutDisputeAsync(string payoutId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("{Prefix} Открытие диспута для выплаты: {PayoutId}", Prefix, payoutId);

                var response = await _httpClient.PostAsync($"api/payout/{payoutId}/dispute", null, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<ProcessingDisputeApiResponse>>(content, JsonOptions);
                    if (result?.Success is true && result.Data is not null)
                    {
                        return new DisputeResult(true, result.Data.Uuid);
                    }
                }

                var errorResult = JsonSerializer.Deserialize<ProcessingApiErrorResponse>(content, JsonOptions);
                return new DisputeResult(false, null, errorResult?.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при открытии диспута для {PayoutId}", Prefix, payoutId);
                return new DisputeResult(false, null, ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<DisputeDetails?> GetPayoutDisputeAsync(string payoutId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/payout/{payoutId}/dispute", cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<ProcessingDisputeApiResponse>>(content, JsonOptions);
                    if (result?.Success is true && result.Data is not null)
                    {
                        return new DisputeDetails(
                            result.Data.Uuid,
                            result.Data.PayoutId,
                            result.Data.Status,
                            result.Data.CreatedAt,
                            result.Data.ResolvedAt,
                            result.Data.Resolution);
                    }
                }

                _logger.LogWarning("{Prefix} Диспут не найден для выплаты: {PayoutId}", Prefix, payoutId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Исключение при получении диспута для {PayoutId}", Prefix, payoutId);
                return null;
            }
        }

        #endregion Выплаты

        #region Приватные методы

        /// <summary>
        /// Парсит ответ на создание выплаты.
        /// </summary>
        /// <param name="content">Содержимое ответа.</param>
        /// <param name="isSuccessStatusCode">Признак успешного HTTP-статуса.</param>
        /// <returns>Результат создания выплаты.</returns>
        private CreatePayoutResult ParsePayoutResponse(string content, bool isSuccessStatusCode)
        {
            if (isSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ProcessingApiSuccessResponse<ProcessingPayoutApiResponse>>(content, JsonOptions);
                if (result?.Success is true && result.Data is not null)
                {
                    _logger.LogInformation("{Prefix} Выплата создана успешно: UUID={Uuid}", Prefix, result.Data.Uuid);
                    return new CreatePayoutResult(true, result.Data.Uuid, result.Data.Status);
                }
            }

            var errorResult = JsonSerializer.Deserialize<ProcessingApiErrorResponse>(content, JsonOptions);
            var errorMessage = errorResult?.Message ?? "Неизвестная ошибка";

            _logger.LogWarning("{Prefix} Ошибка создания выплаты: {Error}", Prefix, errorMessage);
            return new CreatePayoutResult(false, null, null, errorMessage);
        }

        /// <summary>
        /// Преобразует API-ответ об ордере в доменную модель.
        /// </summary>
        /// <param name="data">Данные API-ответа.</param>
        /// <returns>Детали ордера.</returns>
        private static OrderDetails MapToOrderDetails(ProcessingOrderApiResponse data)
        {
            return new OrderDetails(
                data.Uuid,
                data.ExternalId,
                data.Amount,
                data.Currency,
                data.Status,
                data.SubStatus,
                data.PaymentGateway,
                data.CreatedAt,
                data.ExpiredAt,
                data.PaymentUrl,
                data.ExchangeRate);
        }

        /// <summary>
        /// Преобразует API-ответ о выплате в доменную модель.
        /// </summary>
        /// <param name="data">Данные API-ответа.</param>
        /// <returns>Детали выплаты.</returns>
        private static PayoutDetails MapToPayoutDetails(ProcessingPayoutApiResponse data)
        {
            return new PayoutDetails(
                data.Uuid,
                data.ExternalId,
                data.Amount,
                data.Currency,
                data.Status,
                data.SubStatus,
                data.PaymentGateway,
                data.Wallet,
                data.CreatedAt,
                data.ProcessedAt);
        }

        #endregion Приватные методы
    }
}

