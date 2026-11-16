using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Player settings
    /// </summary>
    [Table("PlayersOptions")]
    public class PlayerOptions
    {
        /// <summary>
        /// Player identifier (primary key)
        /// </summary>
        [Key]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Personal win chance
        /// </summary>
        [Required]
        [Column("WinChance")]
        public float WinChance { get; set; }

        /// <summary>
        /// Wallet for payouts
        /// </summary>
        [Column("Wallet")]
        [MaxLength(100)]
        public string? Wallet { get; set; }

        /// <summary>
        /// Currency code (for example, "rub", "kzt").
        /// Values are loaded dynamically from the processing
        /// </summary>
        [Column("Currency")]
        [MaxLength(10)]
        public string? Currency { get; set; }

        /// <summary>
        /// Payout method code (for example, "sbp_rub", "bankcard_rub").
        /// Values are loaded dynamically from the processing
        /// </summary>
        [Required]
        [Column("PayoutMethod")]
        [MaxLength(50)]
        public string PayoutMethod { get; set; } = string.Empty;

        /// <summary>
        /// Payment method code (for example, "sbp_rub", "bankcard_rub").
        /// Values are loaded dynamically from the processing
        /// </summary>
        [Required]
        [Column("PaymentMethod")]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Type of payment details (for example, "card", "phone", "sbp").
        /// Available values depend on the selected PaymentMethod
        /// </summary>
        [Required]
        [Column("PaymentDetailType")]
        [MaxLength(50)]
        public string PaymentDetailType { get; set; } = "card";

        #region Navigation Properties

        /// <summary>
        /// Related player
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
