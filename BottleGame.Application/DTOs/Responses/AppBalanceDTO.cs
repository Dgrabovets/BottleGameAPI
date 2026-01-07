namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// DTO баланса приложения
    /// </summary>
    public class AppBalanceDTO
    {
        /// <summary>
        /// Сумма на балансе
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Дата обновления суммы баланса
        /// </summary>
        public DateTime? BalanceAmountUpdatedAt { get; set; }

        /// <summary>
        /// ID админа, обновившего сумму баланса
        /// </summary>
        public int? BalanceAmountAdminIdUpdatedBy { get; set; }

        /// <summary>
        /// Логин админа, обновившего сумму баланса
        /// </summary>
        public string? BalanceAmountAdminLoginUpdatedBy { get; set; }

        /// <summary>
        /// Нижний порог баланса
        /// </summary>
        public decimal LowerThreshold { get; set; }

        /// <summary>
        /// Дата обновления нижнего порога
        /// </summary>
        public DateTime? LowerThresholdUpdatedAt { get; set; }

        /// <summary>
        /// ID админа, обновившего нижний порог
        /// </summary>
        public int? LowerThresholdAdminIdUpdatedBy { get; set; }

        /// <summary>
        /// Логин админа, обновившего нижний порог
        /// </summary>
        public string? LowerThresholdAdminLoginUpdatedBy { get; set; }
    }
}
