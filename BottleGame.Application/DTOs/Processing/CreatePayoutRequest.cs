namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Запрос на создание выплаты (вывода) у платёжного провайдера
    /// </summary>
    /// <param name="TransactionId">Идентификатор транзакции в приложении</param>
    /// <param name="Amount">Сумма выплаты</param>
    /// <param name="Currency">Код валюты</param>
    /// <param name="Gateway">Код способа выплаты</param>
    /// <param name="Destination">Реквизиты получателя в формате выбранного способа выплаты</param>
    /// <param name="Description">Описание выплаты</param>
    public record CreatePayoutRequest(int TransactionId, decimal Amount, string Currency, string Gateway, string Destination, string Description);
}
