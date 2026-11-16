using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Game round
    /// </summary>
    [Table("GameRounds")]
    public class GameRound
    {
        /// <summary>
        /// Round identifier
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
        /// Bet size
        /// </summary>
        [Required]
        [Column("Bet")]
        public decimal Bet { get; set; }

        /// <summary>
        /// Whether the round was won
        /// </summary>
        [Required]
        [Column("IsWin")]
        public bool IsWin { get; set; }

        /// <summary>
        /// Date the round finished
        /// </summary>
        [Required]
        [Column("FinishedAt")]
        public DateTime FinishedAt { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Related player
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
