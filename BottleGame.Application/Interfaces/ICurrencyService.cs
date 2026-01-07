using BottleGame.Application.DTOs.Processing;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Сервис для работы с валютами и курсами обмена.
    /// Предоставляет кэшированные справочники валют и платёжных шлюзов.
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>
        /// Получает список доступных валют (с кэшированием).
        /// </summary>
        /// <param name="forceRefresh">Принудительно обновить кэш.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Коллекция информации о валютах.</returns>
        Task<IEnumerable<CurrencyInfo>> GetAvailableCurrenciesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получает список доступных платёжных шлюзов (с кэшированием).
        /// </summary>
        /// <param name="forceRefresh">Принудительно обновить кэш.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Коллекция информации о платёжных шлюзах.</returns>
        Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получает платёжные шлюзы, поддерживающие приём платежей для указанной валюты.
        /// </summary>
        /// <param name="currencyCode">Код валюты (например, "rub").</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Коллекция платёжных шлюзов.</returns>
        Task<IEnumerable<PaymentGatewayInfo>> GetPaymentGatewaysForCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получает платёжные шлюзы, поддерживающие выплаты для указанной валюты.
        /// </summary>
        /// <param name="currencyCode">Код валюты (например, "rub").</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Коллекция платёжных шлюзов.</returns>
        Task<IEnumerable<PaymentGatewayInfo>> GetPayoutGatewaysForCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получает текущий курс обмена для указанной валюты.
        /// </summary>
        /// <param name="currencyCode">Код валюты (например, "rub").</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Курс обмена или 0, если не удалось получить.</returns>
        Task<decimal> GetExchangeRateAsync(string currencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Конвертирует сумму из одной валюты в базовую валюту по текущему курсу.
        /// </summary>
        /// <param name="amount">Сумма для конвертации.</param>
        /// <param name="fromCurrencyCode">Код исходной валюты.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Сконвертированная сумма или исходная сумма, если курс недоступен.</returns>
        Task<decimal> ConvertToBaseCurrencyAsync(decimal amount, string fromCurrencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Конвертирует сумму из базовой валюты в указанную валюту по текущему курсу.
        /// </summary>
        /// <param name="amount">Сумма для конвертации.</param>
        /// <param name="toCurrencyCode">Код целевой валюты.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Сконвертированная сумма или исходная сумма, если курс недоступен.</returns>
        Task<decimal> ConvertFromBaseCurrencyAsync(decimal amount, string toCurrencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет, активна ли указанная валюта.
        /// </summary>
        /// <param name="currencyCode">Код валюты.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>True, если валюта активна.</returns>
        Task<bool> IsCurrencyActiveAsync(string currencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет, поддерживает ли шлюз указанную сумму.
        /// </summary>
        /// <param name="gatewayCode">Код платёжного шлюза.</param>
        /// <param name="amount">Сумма для проверки.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>True, если сумма в допустимых пределах.</returns>
        Task<bool> IsAmountValidForGatewayAsync(string gatewayCode, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Очищает все кэши справочников.
        /// </summary>
        void ClearCache();
    }
}
