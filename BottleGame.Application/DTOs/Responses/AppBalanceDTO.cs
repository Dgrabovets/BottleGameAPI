namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Application balance DTO
    /// </summary>
    public class AppBalanceDTO
    {
        /// <summary>
        /// Amount on the balance
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Date the balance amount was updated
        /// </summary>
        public DateTime? BalanceAmountUpdatedAt { get; set; }

        /// <summary>
        /// Id of the admin who updated the balance amount
        /// </summary>
        public int? BalanceAmountAdminIdUpdatedBy { get; set; }

        /// <summary>
        /// Login of the admin who updated the balance amount
        /// </summary>
        public string? BalanceAmountAdminLoginUpdatedBy { get; set; }

        /// <summary>
        /// Lower threshold of the balance
        /// </summary>
        public decimal LowerThreshold { get; set; }

        /// <summary>
        /// Date the lower threshold was updated
        /// </summary>
        public DateTime? LowerThresholdUpdatedAt { get; set; }

        /// <summary>
        /// Id of the admin who updated the lower threshold
        /// </summary>
        public int? LowerThresholdAdminIdUpdatedBy { get; set; }

        /// <summary>
        /// Login of the admin who updated the lower threshold
        /// </summary>
        public string? LowerThresholdAdminLoginUpdatedBy { get; set; }
    }
}
