namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Результат создания выплаты
    /// </summary>
    /// <param name="IsSuccess">Выплата создана</param>
    /// <param name="PayoutId">Идентификатор выплаты у провайдера</param>
    /// <param name="Error">Описание ошибки</param>
    public record CreatePayoutResult(bool IsSuccess, string? PayoutId, string? Error);
}
