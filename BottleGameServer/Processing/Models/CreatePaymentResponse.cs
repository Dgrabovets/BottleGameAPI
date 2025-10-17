namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Модель ответа на создание платежа на процессинговой площадке
    /// </summary>
    /// <param name="payment_id">ID платежа в системе площадки</param>
    /// <param name="amount">Сумма платежа</param>
    /// <param name="currency">Валюта платежа</param>
    /// <param name="link">Ссылка на оплату</param>
    /// <param name="expired">Время истечения платежа и переведения его в статус "Expired"</param>
    public record CreatePaymentResponse(
        string payment_id,
        int amount,
        string currency,
        string link,
        long expired
    );
}