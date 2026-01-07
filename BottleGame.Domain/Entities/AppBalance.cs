using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Баланс приложения
    /// </summary>
    [Table("AppBalance")]
    public class AppBalance
    {
        /// <summary>
        /// Идентификатор записи
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Сумма на балансе приложения
        /// </summary>
        [Required]
        [Column("Amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Нижний порог баланса
        /// </summary>
        [Required]
        [Column("LowerThreshold")]
        public decimal LowerThreshold { get; set; }

        /// <summary>
        /// Дата обновления нижнего порога
        /// </summary>
        [Column("LowerThresholdUpdatedAt")]
        public DateTime? LowerThresholdUpdatedAt { get; set; }

        /// <summary>
        /// Идентификатор админа, обновившего нижний порог
        /// </summary>
        [Column("LowerThresholdAdminIdUpdatedBy")]
        public int? LowerThresholdAdminIdUpdatedBy { get; set; }

        /// <summary>
        /// Дата обновления суммы баланса
        /// </summary>
        [Column("BalanceAmountUpdatedAt")]
        public DateTime? BalanceAmountUpdatedAt { get; set; }

        /// <summary>
        /// Идентификатор админа, обновившего сумму баланса
        /// </summary>
        [Column("BalanceAmountAdminIdUpdatedBy")]
        public int? BalanceAmountAdminIdUpdatedBy { get; set; }
    }
}
