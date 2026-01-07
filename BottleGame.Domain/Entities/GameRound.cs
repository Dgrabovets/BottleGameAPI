using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Игровой раунд
    /// </summary>
    [Table("GameRounds")]
    public class GameRound
    {
        /// <summary>
        /// Идентификатор раунда
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
        /// Размер ставки
        /// </summary>
        [Required]
        [Column("Bet")]
        public decimal Bet { get; set; }

        /// <summary>
        /// Признак выигрыша
        /// </summary>
        [Required]
        [Column("IsWin")]
        public bool IsWin { get; set; }

        /// <summary>
        /// Дата завершения раунда
        /// </summary>
        [Required]
        [Column("FinishedAt")]
        public DateTime FinishedAt { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Связанный игрок
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
