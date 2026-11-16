namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Упрощённое представление транзакции для агрегации временной шкалы
    /// </summary>
    /// <param name="TypeId">Идентификатор типа транзакции</param>
    /// <param name="Amount">Сумма транзакции</param>
    /// <param name="EffectiveAt">Дата успешной обработки</param>
    internal sealed record TransactionTimelineItem(
        int TypeId,
        decimal Amount,
        DateTime EffectiveAt);
}
