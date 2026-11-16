namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Запрос на создание платежа (пополнения) у платёжного провайдера
    /// </summary>
    /// <param name="TransactionId">Идентификатор транзакции в приложении</param>
    /// <param name="Amount">Сумма платежа</param>
    /// <param name="Currency">Код валюты</param>
    /// <param name="Description">Описание платежа</param>
    public record CreatePaymentRequest(int TransactionId, decimal Amount, string Currency, string Description);
}
