using BottleGame.Application.DTOs.Processing;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Платёжный провайдер: приём платежей, выплаты и справочники
    /// </summary>
    public interface IPaymentProvider
    {
        /// <summary>
        /// Возвращает валюты, с которыми работает провайдер
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Коллекция валют</returns>
        Task<IEnumerable<CurrencyInfo>> GetCurrenciesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает способы приёма платежей и выплат
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Коллекция способов</returns>
        Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает курс валюты к базовой валюте приложения
        /// </summary>
        /// <param name="currency">Код валюты</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Курс; 0, если валюта не поддерживается</returns>
        Task<decimal> GetExchangeRateAsync(string currency, CancellationToken cancellationToken = default);

        /// <summary>
        /// Создаёт платёж. Повторный вызов для той же транзакции возвращает тот же платёж
        /// </summary>
        /// <param name="request">Запрос на создание платежа</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат создания платежа</returns>
        Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Создаёт выплату. Повторный вызов для той же транзакции возвращает ту же выплату
        /// </summary>
        /// <param name="request">Запрос на создание выплаты</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат создания выплаты</returns>
        Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Запрашивает у провайдера текущее состояние платежа
        /// </summary>
        /// <param name="paymentId">Идентификатор платежа у провайдера</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Платёж или null, если провайдер его не знает</returns>
        Task<ProviderOperation?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Запрашивает у провайдера текущее состояние выплаты
        /// </summary>
        /// <param name="payoutId">Идентификатор выплаты у провайдера</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Выплата или null, если провайдер её не знает</returns>
        Task<ProviderOperation?> GetPayoutAsync(string payoutId, CancellationToken cancellationToken = default);
    }
}
