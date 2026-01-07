using BottleGame.Application.DTOs.Processing;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Сервис интеграции с платёжным процессингом.
    /// </summary>
    public interface IProcessingService
    {
        #region Справочники

        /// <summary>
        /// Получить список доступных валют.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Коллекция валют.</returns>
        Task<IEnumerable<CurrencyInfo>> GetCurrenciesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить список доступных платёжных методов (шлюзов).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Коллекция платёжных шлюзов.</returns>
        Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить курс конвертации для мерчанта.
        /// </summary>
        /// <param name="currency">Код валюты (например, "rub").</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Курс обмена.</returns>
        Task<decimal> GetMerchantExchangeRateAsync(string currency, CancellationToken cancellationToken = default);

        #endregion Справочники

        #region Сделки (Orders/Deposits)

        /// <summary>
        /// Создать новый ордер (депозит) через Merchant API.
        /// </summary>
        /// <param name="request">Запрос на создание ордера.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Результат создания ордера.</returns>
        Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить информацию об ордере по UUID.
        /// </summary>
        /// <param name="orderId">UUID ордера.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Детали ордера или null.</returns>
        Task<OrderDetails?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить информацию об ордере по external_id.
        /// </summary>
        /// <param name="externalId">Внешний ID ордера.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Детали ордера или null.</returns>
        Task<OrderDetails?> GetOrderByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Отменить ордер.
        /// </summary>
        /// <param name="orderId">UUID ордера.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Успешность отмены.</returns>
        Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Подтвердить ордер (для H2H API).
        /// </summary>
        /// <param name="orderId">UUID ордера.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Успешность подтверждения.</returns>
        Task<bool> ConfirmOrderAsync(string orderId, CancellationToken cancellationToken = default);

        #endregion Сделки

        #region Выплаты (Payouts)

        /// <summary>
        /// Создать прямую выплату (direct payout).
        /// </summary>
        /// <param name="request">Запрос на создание выплаты.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Результат создания выплаты.</returns>
        Task<CreatePayoutResult> CreateDirectPayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Создать выплату через пул (pool payout).
        /// </summary>
        /// <param name="request">Запрос на создание выплаты через пул.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Результат создания выплаты.</returns>
        Task<CreatePayoutResult> CreatePoolPayoutAsync(CreatePoolPayoutRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить информацию о выплате по UUID.
        /// </summary>
        /// <param name="payoutId">UUID выплаты.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Детали выплаты или null.</returns>
        Task<PayoutDetails?> GetPayoutAsync(string payoutId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Открыть диспут по выплате.
        /// </summary>
        /// <param name="payoutId">UUID выплаты.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Результат открытия диспута.</returns>
        Task<DisputeResult> OpenPayoutDisputeAsync(string payoutId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить информацию о диспуте выплаты.
        /// </summary>
        /// <param name="payoutId">UUID выплаты.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Детали диспута или null.</returns>
        Task<DisputeDetails?> GetPayoutDisputeAsync(string payoutId, CancellationToken cancellationToken = default);

        #endregion Выплаты
    }
}
