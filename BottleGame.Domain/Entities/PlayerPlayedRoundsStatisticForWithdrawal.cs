using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Статистика сыгранных раундов для вывода средств
    /// </summary>
    [Table("PlayersPlayedRoundsStatisticsForWithdrawals")]
    public class PlayerPlayedRoundsStatisticForWithdrawal
    {
        /// <summary>
        /// Идентификатор записи
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор игрока
        /// </summary>
        [Required]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Идентификатор транзакции депозита
        /// </summary>
        [ForeignKey("MoneyTransaction")]
        [Column("DepositTransactionId")]
        public int? DepositTransactionId { get; set; }

        /// <summary>
        /// Количество сыгранных раундов
        /// </summary>
        [Required]
        [Column("PlayedRoundsCount")]
        public int PlayedRoundsCount { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Связанный игрок
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        /// <summary>
        /// Связанная транзакция депозита
        /// </summary>
        public virtual MoneyTransaction? MoneyTransaction { get; set; }

        #endregion Navigation Properties
    }
}
