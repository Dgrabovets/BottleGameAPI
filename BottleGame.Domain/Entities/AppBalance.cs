using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Application balance
    /// </summary>
    [Table("AppBalance")]
    public class AppBalance
    {
        /// <summary>
        /// Record identifier
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Amount on the application balance
        /// </summary>
        [Required]
        [Column("Amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Lower threshold of the balance
        /// </summary>
        [Required]
        [Column("LowerThreshold")]
        public decimal LowerThreshold { get; set; }

        /// <summary>
        /// Date the lower threshold was updated
        /// </summary>
        [Column("LowerThresholdUpdatedAt")]
        public DateTime? LowerThresholdUpdatedAt { get; set; }

        /// <summary>
        /// Identifier of the admin who updated the lower threshold
        /// </summary>
        [Column("LowerThresholdAdminIdUpdatedBy")]
        public int? LowerThresholdAdminIdUpdatedBy { get; set; }

        /// <summary>
        /// Date the balance amount was updated
        /// </summary>
        [Column("BalanceAmountUpdatedAt")]
        public DateTime? BalanceAmountUpdatedAt { get; set; }

        /// <summary>
        /// Identifier of the admin who updated the balance amount
        /// </summary>
        [Column("BalanceAmountAdminIdUpdatedBy")]
        public int? BalanceAmountAdminIdUpdatedBy { get; set; }

        #region Concurrency

        /// <summary>
        /// Row version for optimistic concurrency. In PostgreSQL this is the xmin system column,
        /// so no separate column or migration is needed. Two requests that change the same balance at once
        /// cannot both win: the second one fails instead of overwriting the first
        /// </summary>
        public uint Version { get; set; }

        #endregion Concurrency
    }
}
