namespace BottleGame.Application.DTOs.Processing
{
    #region Справочники

    /// <summary>
    /// Информация о валюте из процессинга.
    /// </summary>
    public class CurrencyInfo
    {
        /// <summary>
        /// Код валюты (например, "rub").
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Название валюты.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Активна ли валюта.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CurrencyInfo"/>.
        /// </summary>
        public CurrencyInfo()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CurrencyInfo"/>.
        /// </summary>
        /// <param name="code">Код валюты.</param>
        /// <param name="name">Название валюты.</param>
        /// <param name="isActive">Активна ли валюта.</param>
        public CurrencyInfo(string code, string name, bool isActive)
        {
            Code = code;
            Name = name;
            IsActive = isActive;
        }
    }

    /// <summary>
    /// Информация о платёжном шлюзе.
    /// </summary>
    public class PaymentGatewayInfo
    {
        /// <summary>
        /// Код шлюза.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Название шлюза.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Код валюты.
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Поддерживает ли платежи.
        /// </summary>
        public bool SupportsPayments { get; set; }

        /// <summary>
        /// Поддерживает ли выплаты.
        /// </summary>
        public bool SupportsPayouts { get; set; }

        /// <summary>
        /// Минимальная сумма.
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Максимальная сумма.
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Типы платёжных реквизитов, доступные для этого шлюза (например, "card", "phone", "account_number").
        /// </summary>
        public List<string> DetailTypes { get; set; } = [];

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PaymentGatewayInfo"/>.
        /// </summary>
        public PaymentGatewayInfo()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PaymentGatewayInfo"/>.
        /// </summary>
        /// <param name="code">Код шлюза.</param>
        /// <param name="name">Название шлюза.</param>
        /// <param name="currency">Код валюты.</param>
        /// <param name="supportsPayments">Поддерживает ли платежи.</param>
        /// <param name="supportsPayouts">Поддерживает ли выплаты.</param>
        /// <param name="minAmount">Минимальная сумма.</param>
        /// <param name="maxAmount">Максимальная сумма.</param>
        /// <param name="detailTypes">Типы платёжных реквизитов.</param>
        public PaymentGatewayInfo(string code, string name, string currency, bool supportsPayments, bool supportsPayouts, decimal? minAmount, decimal? maxAmount, List<string>? detailTypes = null)
        {
            Code = code;
            Name = name;
            Currency = currency;
            SupportsPayments = supportsPayments;
            SupportsPayouts = supportsPayouts;
            MinAmount = minAmount;
            MaxAmount = maxAmount;
            DetailTypes = detailTypes ?? [];
        }
    }

    #endregion Справочники

    #region Ордера (Orders)

    /// <summary>
    /// Запрос на создание ордера.
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>
        /// UUID мерчанта.
        /// </summary>
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Внешний ID заказа (ID транзакции в нашей системе).
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма в базовой валюте.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Код платёжного шлюза (например, "sberbank").
        /// </summary>
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Тип платёжных реквизитов ("card", "phone", "sbp").
        /// </summary>
        public string PaymentDetailType { get; set; } = "card";

        /// <summary>
        /// ID клиента (Telegram ID).
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// URL для callback-уведомлений.
        /// </summary>
        public string? CallbackUrl { get; set; }

        /// <summary>
        /// URL для редиректа при успехе.
        /// </summary>
        public string? SuccessUrl { get; set; }

        /// <summary>
        /// URL для редиректа при ошибке.
        /// </summary>
        public string? FailUrl { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateOrderRequest"/>.
        /// </summary>
        public CreateOrderRequest()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateOrderRequest"/>.
        /// </summary>
        /// <param name="merchantId">UUID мерчанта.</param>
        /// <param name="externalId">Внешний ID заказа.</param>
        /// <param name="amount">Сумма.</param>
        /// <param name="paymentGateway">Код шлюза.</param>
        /// <param name="paymentDetailType">Тип реквизитов.</param>
        /// <param name="clientId">ID клиента.</param>
        /// <param name="callbackUrl">URL для callback.</param>
        /// <param name="successUrl">URL при успехе.</param>
        /// <param name="failUrl">URL при ошибке.</param>
        public CreateOrderRequest(
            string merchantId,
            string externalId,
            decimal amount,
            string paymentGateway,
            string paymentDetailType = "card",
            string? clientId = null,
            string? callbackUrl = null,
            string? successUrl = null,
            string? failUrl = null)
        {
            MerchantId = merchantId;
            ExternalId = externalId;
            Amount = amount;
            PaymentGateway = paymentGateway;
            PaymentDetailType = paymentDetailType;
            ClientId = clientId;
            CallbackUrl = callbackUrl;
            SuccessUrl = successUrl;
            FailUrl = failUrl;
        }
    }

    /// <summary>
    /// Результат создания ордера.
    /// </summary>
    public class CreateOrderResult
    {
        /// <summary>
        /// Успешно ли создан ордер.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// UUID созданного ордера.
        /// </summary>
        public string? OrderId { get; set; }

        /// <summary>
        /// URL для оплаты.
        /// </summary>
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// Сообщение об ошибке.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateOrderResult"/>.
        /// </summary>
        public CreateOrderResult()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateOrderResult"/>.
        /// </summary>
        /// <param name="isSuccess">Успешно ли создан.</param>
        /// <param name="orderId">UUID ордера.</param>
        /// <param name="paymentUrl">URL для оплаты.</param>
        /// <param name="errorMessage">Сообщение об ошибке.</param>
        public CreateOrderResult(bool isSuccess, string? orderId, string? paymentUrl, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            OrderId = orderId;
            PaymentUrl = paymentUrl;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Детали ордера.
    /// </summary>
    public class OrderDetails
    {
        /// <summary>
        /// UUID ордера.
        /// </summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// Внешний ID.
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Валюта.
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Статус.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Под-статус.
        /// </summary>
        public string SubStatus { get; set; } = string.Empty;

        /// <summary>
        /// Платёжный шлюз.
        /// </summary>
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата истечения.
        /// </summary>
        public DateTime? ExpiredAt { get; set; }

        /// <summary>
        /// URL для оплаты.
        /// </summary>
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// Курс обмена.
        /// </summary>
        public decimal? ExchangeRate { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="OrderDetails"/>.
        /// </summary>
        public OrderDetails()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="OrderDetails"/>.
        /// </summary>
        /// <param name="orderId">UUID ордера.</param>
        /// <param name="externalId">Внешний ID.</param>
        /// <param name="amount">Сумма.</param>
        /// <param name="currency">Валюта.</param>
        /// <param name="status">Статус.</param>
        /// <param name="subStatus">Под-статус.</param>
        /// <param name="paymentGateway">Шлюз.</param>
        /// <param name="createdAt">Дата создания.</param>
        /// <param name="expiredAt">Дата истечения.</param>
        /// <param name="paymentUrl">URL для оплаты.</param>
        /// <param name="exchangeRate">Курс обмена.</param>
        public OrderDetails(
            string orderId,
            string externalId,
            decimal amount,
            string currency,
            string status,
            string subStatus,
            string paymentGateway,
            DateTime createdAt,
            DateTime? expiredAt,
            string? paymentUrl,
            decimal? exchangeRate)
        {
            OrderId = orderId;
            ExternalId = externalId;
            Amount = amount;
            Currency = currency;
            Status = status;
            SubStatus = subStatus;
            PaymentGateway = paymentGateway;
            CreatedAt = createdAt;
            ExpiredAt = expiredAt;
            PaymentUrl = paymentUrl;
            ExchangeRate = exchangeRate;
        }
    }

    #endregion Ордера

    #region Выплаты (Payouts)

    /// <summary>
    /// Запрос на создание выплаты (прямой).
    /// </summary>
    public class CreatePayoutRequest
    {
        /// <summary>
        /// UUID мерчанта.
        /// </summary>
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Внешний ID выплаты (ID транзакции в нашей системе).
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма выплаты.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Кошелёк/карта для выплаты.
        /// </summary>
        public string Wallet { get; set; } = string.Empty;

        /// <summary>
        /// Код валюты.
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// URL для callback-уведомлений.
        /// </summary>
        public string? CallbackUrl { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreatePayoutRequest"/>.
        /// </summary>
        public CreatePayoutRequest()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreatePayoutRequest"/>.
        /// </summary>
        /// <param name="merchantId">UUID мерчанта.</param>
        /// <param name="externalId">Внешний ID.</param>
        /// <param name="amount">Сумма.</param>
        /// <param name="paymentGateway">Код шлюза.</param>
        /// <param name="wallet">Кошелёк/карта.</param>
        /// <param name="currency">Код валюты.</param>
        /// <param name="callbackUrl">URL для callback.</param>
        public CreatePayoutRequest(
            string merchantId,
            string externalId,
            decimal amount,
            string paymentGateway,
            string wallet,
            string currency,
            string? callbackUrl = null)
        {
            MerchantId = merchantId;
            ExternalId = externalId;
            Amount = amount;
            PaymentGateway = paymentGateway;
            Wallet = wallet;
            Currency = currency;
            CallbackUrl = callbackUrl;
        }
    }

    /// <summary>
    /// Запрос на создание выплаты через пул.
    /// </summary>
    public class CreatePoolPayoutRequest
    {
        /// <summary>
        /// UUID мерчанта.
        /// </summary>
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Внешний ID выплаты.
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма выплаты.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Кошелёк/карта для выплаты.
        /// </summary>
        public string Wallet { get; set; } = string.Empty;

        /// <summary>
        /// Код валюты.
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Время ожидания в пуле (минуты).
        /// </summary>
        public int WaitingTimeMinutes { get; set; } = 30;

        /// <summary>
        /// URL для callback-уведомлений.
        /// </summary>
        public string? CallbackUrl { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreatePoolPayoutRequest"/>.
        /// </summary>
        public CreatePoolPayoutRequest()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreatePoolPayoutRequest"/>.
        /// </summary>
        /// <param name="merchantId">UUID мерчанта.</param>
        /// <param name="externalId">Внешний ID.</param>
        /// <param name="amount">Сумма.</param>
        /// <param name="paymentGateway">Код шлюза.</param>
        /// <param name="wallet">Кошелёк/карта.</param>
        /// <param name="currency">Код валюты.</param>
        /// <param name="waitingTimeMinutes">Время ожидания в минутах.</param>
        /// <param name="callbackUrl">URL для callback.</param>
        public CreatePoolPayoutRequest(
            string merchantId,
            string externalId,
            decimal amount,
            string paymentGateway,
            string wallet,
            string currency,
            int waitingTimeMinutes = 30,
            string? callbackUrl = null)
        {
            MerchantId = merchantId;
            ExternalId = externalId;
            Amount = amount;
            PaymentGateway = paymentGateway;
            Wallet = wallet;
            Currency = currency;
            WaitingTimeMinutes = waitingTimeMinutes;
            CallbackUrl = callbackUrl;
        }
    }

    /// <summary>
    /// Результат создания выплаты.
    /// </summary>
    public class CreatePayoutResult
    {
        /// <summary>
        /// Успешно ли создана выплата.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// UUID выплаты.
        /// </summary>
        public string? PayoutId { get; set; }

        /// <summary>
        /// Статус выплаты.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Сообщение об ошибке.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreatePayoutResult"/>.
        /// </summary>
        public CreatePayoutResult()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreatePayoutResult"/>.
        /// </summary>
        /// <param name="isSuccess">Успешно ли создана.</param>
        /// <param name="payoutId">UUID выплаты.</param>
        /// <param name="status">Статус.</param>
        /// <param name="errorMessage">Сообщение об ошибке.</param>
        public CreatePayoutResult(bool isSuccess, string? payoutId, string? status, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            PayoutId = payoutId;
            Status = status;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Детали выплаты.
    /// </summary>
    public class PayoutDetails
    {
        /// <summary>
        /// UUID выплаты.
        /// </summary>
        public string PayoutId { get; set; } = string.Empty;

        /// <summary>
        /// Внешний ID.
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Валюта.
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Статус.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Под-статус.
        /// </summary>
        public string SubStatus { get; set; } = string.Empty;

        /// <summary>
        /// Платёжный шлюз.
        /// </summary>
        public string PaymentGateway { get; set; } = string.Empty;

        /// <summary>
        /// Кошелёк.
        /// </summary>
        public string Wallet { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата обработки.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PayoutDetails"/>.
        /// </summary>
        public PayoutDetails()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PayoutDetails"/>.
        /// </summary>
        /// <param name="payoutId">UUID выплаты.</param>
        /// <param name="externalId">Внешний ID.</param>
        /// <param name="amount">Сумма.</param>
        /// <param name="currency">Валюта.</param>
        /// <param name="status">Статус.</param>
        /// <param name="subStatus">Под-статус.</param>
        /// <param name="paymentGateway">Шлюз.</param>
        /// <param name="wallet">Кошелёк.</param>
        /// <param name="createdAt">Дата создания.</param>
        /// <param name="processedAt">Дата обработки.</param>
        public PayoutDetails(
            string payoutId,
            string externalId,
            decimal amount,
            string currency,
            string status,
            string subStatus,
            string paymentGateway,
            string wallet,
            DateTime createdAt,
            DateTime? processedAt)
        {
            PayoutId = payoutId;
            ExternalId = externalId;
            Amount = amount;
            Currency = currency;
            Status = status;
            SubStatus = subStatus;
            PaymentGateway = paymentGateway;
            Wallet = wallet;
            CreatedAt = createdAt;
            ProcessedAt = processedAt;
        }
    }

    /// <summary>
    /// Результат открытия диспута.
    /// </summary>
    public class DisputeResult
    {
        /// <summary>
        /// Успешно ли открыт диспут.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// UUID диспута.
        /// </summary>
        public string? DisputeId { get; set; }

        /// <summary>
        /// Сообщение об ошибке.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DisputeResult"/>.
        /// </summary>
        public DisputeResult()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DisputeResult"/>.
        /// </summary>
        /// <param name="isSuccess">Успешно ли открыт.</param>
        /// <param name="disputeId">UUID диспута.</param>
        /// <param name="errorMessage">Сообщение об ошибке.</param>
        public DisputeResult(bool isSuccess, string? disputeId, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            DisputeId = disputeId;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Детали диспута.
    /// </summary>
    public class DisputeDetails
    {
        /// <summary>
        /// UUID диспута.
        /// </summary>
        public string DisputeId { get; set; } = string.Empty;

        /// <summary>
        /// UUID выплаты.
        /// </summary>
        public string PayoutId { get; set; } = string.Empty;

        /// <summary>
        /// Статус диспута.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата разрешения.
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// Результат разрешения.
        /// </summary>
        public string? Resolution { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DisputeDetails"/>.
        /// </summary>
        public DisputeDetails()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DisputeDetails"/>.
        /// </summary>
        /// <param name="disputeId">UUID диспута.</param>
        /// <param name="payoutId">UUID выплаты.</param>
        /// <param name="status">Статус.</param>
        /// <param name="createdAt">Дата создания.</param>
        /// <param name="resolvedAt">Дата разрешения.</param>
        /// <param name="resolution">Результат разрешения.</param>
        public DisputeDetails(
            string disputeId,
            string payoutId,
            string status,
            DateTime createdAt,
            DateTime? resolvedAt,
            string? resolution)
        {
            DisputeId = disputeId;
            PayoutId = payoutId;
            Status = status;
            CreatedAt = createdAt;
            ResolvedAt = resolvedAt;
            Resolution = resolution;
        }
    }

    #endregion Выплаты
}

