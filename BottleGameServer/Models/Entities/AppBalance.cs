using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Модель баланса приложения
    /// </summary>
    public class AppBalance
    {
        /// <summary>
        /// Идентификатор параметров
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Игровой баланс игры
        /// </summary>
        [Column("Amount")]
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Balance must be positive")]
        public int Amount { get; set; }

        /// <summary>
        /// Время пополнения баланса приложения
        /// </summary>
        [Column("BalanceAmountUpdatedAt")]
        public DateTime? BalanceAmountUpdatedAt { get; set; }

        /// <summary>
        /// Кем был последний раз пополнен баланс
        /// </summary>
        [Column("BalanceAmountAdminIdUpdatedBy")]
        public int? BalanceAmountAdminIdUpdatedBy { get; set; }

        /// <summary>
        /// Нижний порог для баланса (НЗ)
        /// </summary>
        [Column("LowerThreshold")]
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Threshold must be positive")]
        public int LowerThreshold { get; set; }

        /// <summary>
        /// Время обновления нижнего порога баланса (НЗ)
        /// </summary>
        [Column("LowerThresholdUpdatedAt")]
        public DateTime? LowerThresholdUpdatedAt { get; set; }

        /// <summary>
        /// Кем был обновлен нижний порог
        /// </summary>
        [Column("LowerThresholdAdminIdUpdatedBy")]
        public int? LowerThresholdAdminIdUpdatedBy { get; set; }
    }
}
