namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Модель запроса на создание выплаты (payout) на процессинговой площадке
    /// </summary>
    /// <param name="merchant_id">ID мерчанта</param>
    /// <param name="secret">Секретный ключ для работы с площадкой процессинга</param>
    /// <param name="order_id">ID выплаты в вашей системе</param>
    /// <param name="balance">Баланс мерчанта для списания (например, "USDT")</param>
    /// <param name="method">Метод выплаты</param>
    /// <param name="wallet">Кошелёк или номер карты</param>
    /// <param name="amount">Сумма выплаты в центах/копейках</param>
    /// <param name="comment">Комментарий к выплате</param>
    /// <param name="fee_merchant">С кого списывать комиссию (true - с мерчанта, false - с выплаты)</param>
    public record CreatePayoutRequest(
        string merchant_id,
        string secret,
        string order_id,
        string balance,
        string method,
        string wallet,
        int amount,
        string? comment,
        bool fee_merchant
    );
}