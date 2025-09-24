using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Статистика сыгранных раундов игрока, привязка к транзакции пополнения
    /// </summary>
    [Table("PlayersPlayedRoundsStatisticsForWithdrawals")]
    public class PlayerPlayedRoundsStatisticForWithdrawal
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// ИД игрока
        /// </summary>
        [Required]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }
        public virtual Player Player { get; set; } = new Player();

        /// <summary>
        /// ИД транзакции пополнения (может быть null, если пополнения ещё не было)
        /// </summary>
        [ForeignKey("MoneyTransaction")]
        [Column("DepositTransactionId")]
        public int? DepositTransactionId { get; set; }
        public virtual MoneyTransaction? MoneyTransaction { get; set; }

        /// <summary>
        /// Количество сыгранных игр
        /// </summary>
        [Required]
        [Column("PlayedRoundsCount")]
        public int PlayedRoundsCount { get; set; }
    }
}