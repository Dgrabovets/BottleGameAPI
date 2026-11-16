namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Платёж или выплата в том виде, в котором их видит провайдер
    /// </summary>
    /// <param name="Id">Идентификатор у провайдера</param>
    /// <param name="Status">Состояние</param>
    /// <param name="Amount">Сумма</param>
    /// <param name="Currency">Код валюты</param>
    /// <param name="TransactionId">Идентификатор транзакции в приложении, сохранённый при создании</param>
    /// <param name="CancellationReason">Причина отмены, если операция отменена</param>
    public record ProviderOperation(
        string Id,
        ProviderOperationStatus Status,
        decimal Amount,
        string Currency,
        int? TransactionId,
        string? CancellationReason);
}
