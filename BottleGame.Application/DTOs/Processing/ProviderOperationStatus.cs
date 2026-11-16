namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Состояние платежа или выплаты у провайдера
    /// </summary>
    public enum ProviderOperationStatus
    {
        /// <summary>
        /// Операция ещё не завершена
        /// </summary>
        Pending,

        /// <summary>
        /// Операция успешно завершена
        /// </summary>
        Succeeded,

        /// <summary>
        /// Операция отменена или отклонена
        /// </summary>
        Canceled
    }
}
