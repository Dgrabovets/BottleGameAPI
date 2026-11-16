using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Statistics of the rounds played for a withdrawal
    /// </summary>
    [Table("PlayersPlayedRoundsStatisticsForWithdrawals")]
    public class PlayerPlayedRoundsStatisticForWithdrawal
    {
        /// <summary>
        /// Record identifier
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Player id
        /// </summary>
        [Required]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Deposit transaction identifier
        /// </summary>
        [ForeignKey("MoneyTransaction")]
        [Column("DepositTransactionId")]
        public int? DepositTransactionId { get; set; }

        /// <summary>
        /// Number of rounds played
        /// </summary>
        [Required]
        [Column("PlayedRoundsCount")]
        public int PlayedRoundsCount { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Related player
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        /// <summary>
        /// Related deposit transaction
        /// </summary>
        public virtual MoneyTransaction? MoneyTransaction { get; set; }

        #endregion Navigation Properties
    }
}
