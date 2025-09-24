using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Данные о балансе игрока
    /// </summary>
    [Table("PlayersBalances")]
    public class Balance
    {
        /// <summary>
        /// Идентификатор игрока
        /// </summary>
        [ForeignKey("Player")]
        [Column("PlayerId")]
        [Required]
        public int PlayerId { get; set; }
        public virtual Player Player { get; set; } = new Player();

        /// <summary>
        /// Игровой баланс игрока
        /// </summary>
        [Column("Amount")]
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Balance must be positive")]
        public int Amount { get; set; }
    }
}
