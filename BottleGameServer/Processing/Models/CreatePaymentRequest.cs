namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Модель запроса на создание платежа на процессинговой площадке
    /// </summary>
    /// <param name="merchant_id">ID мерчанта</param>
    /// <param name="secret">Секретный ключ для работы с площадкой процессинга</param>
    /// <param name="order_id">ID платежа или клиента в системе</param>
    /// <param name="customer">Почта или логин клиента (для более быстрой поддержки клиентов)</param>
    /// <param name="amount">Сумма платежа в центах/копейках. Пример: 125.28 USD это 12528</param>
    /// <param name="currency">Валюта платежа. Доступно: USD, EUR, RUB, UAH, KZT</param>
    /// <param name="description">Описание/комментарий к платежу. Показывается клиенту при оплате</param>
    /// <param name="method">Метод оплаты</param>
    /// <param name="success_url">URL перенаправления при успешной оплате. Если не указано, то открывается сайт мерчанта</param>
    /// <param name="fail_url">URL перенаправления при ошибке при оплате. Если не указано, то открывается сайт мерчанта</param>
    public record CreatePaymentRequest(
        string merchant_id,
        string secret,
        string order_id,
        string customer,
        int amount,
        string currency,
        string? description = null,
        string? method = null,
        string? success_url = null,
        string? fail_url = null
    );
}
