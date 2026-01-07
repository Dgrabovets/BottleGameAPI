using System.Text.Json.Serialization;

namespace BottleGame.Infrastructure.External.Processing
{
    #region Общие модели API

    /// <summary>
    /// Обёртка успешного ответа от платёжного API.
    /// </summary>
    /// <typeparam name="T">Тип данных в ответе.</typeparam>
    public class ProcessingApiSuccessResponse<T>
    {
        /// <summary>
        /// Признак успешности операции.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Данные ответа.
        /// </summary>
        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    /// <summary>
    /// Ответ с ошибкой от платёжного API.
    /// </summary>
    public class ProcessingApiErrorResponse
    {
        /// <summary>
        /// Сообщение об ошибке.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Детальные ошибки валидации по полям.
        /// </summary>
        [JsonPropertyName("errors")]
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    #endregion Общие модели API

    #region Справочники

    /// <summary>
    /// Информация о валюте из платёжного API.
    /// </summary>
    public class ProcessingCurrencyResponse
    {
        /// <summary>
        /// Код валюты (например, "rub", "kzt").
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Точность валюты (количество знаков после запятой).
        /// </summary>
        [JsonPropertyName("precision")]
        public int Precision { get; set; }

        /// <summary>
        /// Символ валюты (например, "₽", "₸").
        /// </summary>
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Название валюты.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Информация о платёжном шлюзе из платёжного API.
    /// </summary>
    public class ProcessingGatewayResponse
    {
        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Название платёжного шлюза.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Код валюты шлюза.
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Поддерживает ли шлюз приём платежей.
        /// </summary>
        [JsonPropertyName("supports_payments")]
        public bool SupportsPayments { get; set; }

        /// <summary>
        /// Поддерживает ли шлюз выплаты.
        /// </summary>
        [JsonPropertyName("supports_payouts")]
        public bool SupportsPayouts { get; set; }

        /// <summary>
        /// Минимальная сумма операции.
        /// </summary>
        [JsonPropertyName("min_amount")]
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Максимальная сумма операции.
        /// </summary>
        [JsonPropertyName("max_amount")]
        public decimal? MaxAmount { get; set; }
    }

    /// <summary>
    /// Курс обмена валюты.
    /// </summary>
    public class ProcessingExchangeRateResponse
    {
        /// <summary>
        /// Код валюты.
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Курс обмена.
        /// </summary>
        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }
    }

    #endregion Справочники

    #region Ордера (Orders)

    /// <summary>
    /// API-запрос на создание ордера (депозита).
    /// </summary>
    public class ProcessingCreateOrderApiRequest
    {
        /// <summary>
        /// UUID мерчанта в платёжной системе.
        /// </summary>
        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Внешний идентификатор заказа (ID транзакции в нашей системе).
        /// </summary>
        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма платежа.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Код платёжного шлюза (например, "sberbank", "alfabank").
        /// </summary>
        [JsonPropertyName("payment_gateway")]
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Тип платёжных реквизитов ("card", "phone", "sbp").
        /// </summary>
        [JsonPropertyName("payment_detail_type")]
        public string PaymentDetailType { get; set; } = "card";

        /// <summary>
        /// ID клиента (например, Telegram ID).
        /// </summary>
        [JsonPropertyName("client_id")]
        public string? ClientId { get; set; }

        /// <summary>
        /// URL для callback-уведомлений об изменении статуса.
        /// </summary>
        [JsonPropertyName("callback_url")]
        public string? CallbackUrl { get; set; }

        /// <summary>
        /// URL для редиректа при успешной оплате.
        /// </summary>
        [JsonPropertyName("success_url")]
        public string? SuccessUrl { get; set; }

        /// <summary>
        /// URL для редиректа при ошибке оплаты.
        /// </summary>
        [JsonPropertyName("fail_url")]
        public string? FailUrl { get; set; }
    }

    /// <summary>
    /// API-ответ с информацией об ордере.
    /// </summary>
    public class ProcessingOrderApiResponse
    {
        /// <summary>
        /// UUID ордера в платёжной системе.
        /// </summary>
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        /// <summary>
        /// Внешний идентификатор заказа.
        /// </summary>
        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма платежа.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Код валюты.
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Статус ордера (pending, success, fail).
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Под-статус ордера.
        /// </summary>
        [JsonPropertyName("sub_status")]
        public string SubStatus { get; set; } = string.Empty;

        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        [JsonPropertyName("payment_gateway")]
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// URL для оплаты.
        /// </summary>
        [JsonPropertyName("payment_url")]
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// Дата создания ордера.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата истечения срока оплаты.
        /// </summary>
        [JsonPropertyName("expired_at")]
        public DateTime? ExpiredAt { get; set; }

        /// <summary>
        /// Курс обмена валюты.
        /// </summary>
        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }
    }

    #endregion Ордера

    #region Выплаты (Payouts)

    /// <summary>
    /// Базовый API-запрос на создание выплаты.
    /// </summary>
    public class ProcessingCreatePayoutApiRequestBase
    {
        /// <summary>
        /// UUID мерчанта в платёжной системе.
        /// </summary>
        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Внешний идентификатор выплаты (ID транзакции в нашей системе).
        /// </summary>
        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма выплаты.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Кошелёк/карта для выплаты.
        /// </summary>
        [JsonPropertyName("wallet")]
        public string Wallet { get; set; } = string.Empty;

        /// <summary>
        /// Код валюты.
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// URL для callback-уведомлений об изменении статуса.
        /// </summary>
        [JsonPropertyName("callback_url")]
        public string? CallbackUrl { get; set; }
    }

    /// <summary>
    /// API-запрос на создание прямой выплаты.
    /// </summary>
    public class ProcessingDirectPayoutApiRequest : ProcessingCreatePayoutApiRequestBase
    {
    }

    /// <summary>
    /// API-запрос на создание выплаты через пул.
    /// </summary>
    public class ProcessingPoolPayoutApiRequest : ProcessingCreatePayoutApiRequestBase
    {
        /// <summary>
        /// Время ожидания в пуле (минуты).
        /// </summary>
        [JsonPropertyName("waiting_time")]
        public int WaitingTime { get; set; } = 30;
    }

    /// <summary>
    /// API-ответ с информацией о выплате.
    /// </summary>
    public class ProcessingPayoutApiResponse
    {
        /// <summary>
        /// UUID выплаты в платёжной системе.
        /// </summary>
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        /// <summary>
        /// Внешний идентификатор выплаты.
        /// </summary>
        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма выплаты.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Код валюты.
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Статус выплаты (pending, success, fail).
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Под-статус выплаты.
        /// </summary>
        [JsonPropertyName("sub_status")]
        public string SubStatus { get; set; } = string.Empty;

        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        [JsonPropertyName("payment_gateway")]
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Кошелёк/карта для выплаты.
        /// </summary>
        [JsonPropertyName("wallet")]
        public string Wallet { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания выплаты.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата обработки выплаты.
        /// </summary>
        [JsonPropertyName("processed_at")]
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Признак выплаты через пул (для выплат через пул).
        /// </summary>
        [JsonPropertyName("is_pool")]
        public bool? IsPool { get; set; }

        /// <summary>
        /// Время ожидания в пуле в минутах (для выплат через пул).
        /// </summary>
        [JsonPropertyName("pool_waiting_minutes")]
        public int? PoolWaitingMinutes { get; set; }

        /// <summary>
        /// Unix timestamp до какого момента заявка будет доступна в пуле (для выплат через пул).
        /// </summary>
        [JsonPropertyName("pool_waiting_until")]
        public long? PoolWaitingUntil { get; set; }

        /// <summary>
        /// Unix timestamp когда заявка была взята из пула (для выплат через пул).
        /// </summary>
        [JsonPropertyName("pool_taken_at")]
        public long? PoolTakenAt { get; set; }
    }

    /// <summary>
    /// API-ответ с информацией о диспуте.
    /// </summary>
    public class ProcessingDisputeApiResponse
    {
        /// <summary>
        /// UUID диспута.
        /// </summary>
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        /// <summary>
        /// UUID выплаты, к которой относится диспут.
        /// </summary>
        [JsonPropertyName("payout_id")]
        public string PayoutId { get; set; } = string.Empty;

        /// <summary>
        /// Статус диспута.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания диспута.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата разрешения диспута.
        /// </summary>
        [JsonPropertyName("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// Результат разрешения диспута.
        /// </summary>
        [JsonPropertyName("resolution")]
        public string? Resolution { get; set; }
    }

    #endregion Выплаты

    #region Webhook Payloads

    /// <summary>
    /// Payload вебхука изменения статуса ордера.
    /// </summary>
    public class OrderWebhookPayload
    {
        /// <summary>
        /// UUID ордера.
        /// </summary>
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        /// <summary>
        /// Внешний идентификатор заказа.
        /// </summary>
        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// UUID мерчанта.
        /// </summary>
        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма платежа.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Код валюты.
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Статус ордера.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Под-статус ордера.
        /// </summary>
        [JsonPropertyName("sub_status")]
        public string SubStatus { get; set; } = string.Empty;

        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        [JsonPropertyName("payment_gateway")]
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Курс обмена валюты.
        /// </summary>
        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }

        /// <summary>
        /// Дата создания.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата обновления.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Payload вебхука изменения статуса выплаты.
    /// </summary>
    public class PayoutWebhookPayload
    {
        /// <summary>
        /// UUID выплаты.
        /// </summary>
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        /// <summary>
        /// Внешний идентификатор выплаты.
        /// </summary>
        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// UUID мерчанта.
        /// </summary>
        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма выплаты.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Код валюты.
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Статус выплаты.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Под-статус выплаты.
        /// </summary>
        [JsonPropertyName("sub_status")]
        public string SubStatus { get; set; } = string.Empty;

        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        [JsonPropertyName("payment_gateway")]
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Кошелёк/карта.
        /// </summary>
        [JsonPropertyName("wallet")]
        public string Wallet { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата обновления.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    #endregion Webhook Payloads

    #region Статусы

    /// <summary>
    /// Статусы ордера.
    /// </summary>
    public static class OrderStatus
    {
        /// <summary>
        /// Ожидает оплаты.
        /// </summary>
        public const string Pending = "pending";

        /// <summary>
        /// Успешно оплачен.
        /// </summary>
        public const string Success = "success";

        /// <summary>
        /// Ошибка оплаты.
        /// </summary>
        public const string Fail = "fail";
    }

    /// <summary>
    /// Под-статусы ордера.
    /// </summary>
    public static class OrderSubStatus
    {
        /// <summary>
        /// Ожидает оплаты от клиента.
        /// </summary>
        public const string WaitingForPayment = "waiting_for_payment";

        /// <summary>
        /// Оплата получена.
        /// </summary>
        public const string PaymentReceived = "payment_received";

        /// <summary>
        /// Срок оплаты истёк.
        /// </summary>
        public const string Expired = "expired";

        /// <summary>
        /// Отменён.
        /// </summary>
        public const string Cancelled = "cancelled";
    }

    /// <summary>
    /// Статусы выплаты.
    /// </summary>
    public static class PayoutStatus
    {
        /// <summary>
        /// В обработке.
        /// </summary>
        public const string Pending = "pending";

        /// <summary>
        /// Успешно выполнена.
        /// </summary>
        public const string Success = "success";

        /// <summary>
        /// Ошибка выплаты.
        /// </summary>
        public const string Fail = "fail";
    }

    /// <summary>
    /// Под-статусы выплаты.
    /// </summary>
    public static class PayoutSubStatus
    {
        /// <summary>
        /// Обрабатывается трейдером.
        /// </summary>
        public const string ProcessingByTrader = "processing_by_trader";

        /// <summary>
        /// Ожидает в пуле.
        /// </summary>
        public const string WaitingInPool = "waiting_in_pool";

        /// <summary>
        /// Открыт диспут.
        /// </summary>
        public const string Dispute = "dispute";

        /// <summary>
        /// Завершена полностью (конечная точка).
        /// </summary>
        public const string FullyCompleted = "fully_completed";

        /// <summary>
        /// Ожидает трейдера (для выплат через пул).
        /// </summary>
        public const string WaitingForTrader = "waiting_for_trader";

        /// <summary>
        /// Обрабатывается администратором.
        /// </summary>
        public const string ProcessingByAdministrator = "processing_by_administrator";

        /// <summary>
        /// Отменена по таймауту.
        /// </summary>
        public const string CanceledByTimeout = "canceled_by_timeout";

        /// <summary>
        /// Просрочена.
        /// </summary>
        public const string Expired = "expired";

        /// <summary>
        /// Спор закрыт.
        /// </summary>
        public const string DisputeClosed = "dispute_closed";

        /// <summary>
        /// Отклонена.
        /// </summary>
        public const string Rejected = "rejected";
    }

    #endregion Статусы
}

