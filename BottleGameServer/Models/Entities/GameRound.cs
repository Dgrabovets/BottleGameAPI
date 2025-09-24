using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGameServer.Models.Entities
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
        [ForeignKey("Player")]
        [Column("PlayerId")]
        [Required]
        public int PlayerId { get; set; }
        public virtual Player Player { get; set; } = new Player();

        /// <summary>
        /// Ставка
        /// </summary>
        [Column("Bet")]
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Bet must be positive")]
        public int Bet { get; set; }

        /// <summary>
        /// Признак, выиграл ли игрок
        /// </summary>
        [Column("IsWin")]
        [Required]
        public bool IsWin { get; set; }

        /// <summary>
        /// Время старта раунда
        /// </summary>
        [Column("FinishedAt")]
        [Required]
        public DateTime FinishedAt { get; set; }
    }
}
