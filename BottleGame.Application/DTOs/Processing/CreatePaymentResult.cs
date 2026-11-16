namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Результат создания платежа
    /// </summary>
    /// <param name="IsSuccess">Платёж создан</param>
    /// <param name="PaymentId">Идентификатор платежа у провайдера</param>
    /// <param name="PaymentUrl">Ссылка, на которую нужно отправить игрока для оплаты</param>
    /// <param name="Error">Описание ошибки</param>
    public record CreatePaymentResult(bool IsSuccess, string? PaymentId, string? PaymentUrl, string? Error);
}
