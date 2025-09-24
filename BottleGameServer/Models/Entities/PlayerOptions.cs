using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Сущность Настроек для игрока
    /// </summary>
    [Table("PlayerOptions")]
    public class PlayerOptions
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
        /// Шанс выигрышка игрока
        /// </summary>
        [Column("WinChance")]
        [Range(0, 1)]
        [Required]
        public float WinChance { get; set; }
    }
}
