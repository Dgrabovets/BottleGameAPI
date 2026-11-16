namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// State of a payment or payout at the provider
    /// </summary>
    public enum ProviderOperationStatus
    {
        /// <summary>
        /// The operation is not finished yet
        /// </summary>
        Pending,

        /// <summary>
        /// The operation finished successfully
        /// </summary>
        Succeeded,

        /// <summary>
        /// The operation was canceled or rejected
        /// </summary>
        Canceled
    }
}
