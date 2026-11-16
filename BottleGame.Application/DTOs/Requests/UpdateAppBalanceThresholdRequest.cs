namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Request to update the threshold of the application balance
    /// </summary>
    public class UpdateAppBalanceThresholdRequest
    {
        /// <summary>
        /// New lower threshold
        /// </summary>
        public decimal LowerThreshold { get; set; }
    }
}
