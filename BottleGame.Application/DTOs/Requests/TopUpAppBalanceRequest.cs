namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Request to top up the application balance
    /// </summary>
    public class TopUpAppBalanceRequest
    {
        /// <summary>
        /// Top-up amount
        /// </summary>
        public decimal Amount { get; set; }
    }
}
