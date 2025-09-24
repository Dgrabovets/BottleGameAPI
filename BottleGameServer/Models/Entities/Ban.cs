using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Сущность бана игрока
    /// </summary>
    [Table("Bans")]
    public class Ban
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
        /// Причина бана игрока
        /// </summary>
        [Column("BanReason")]
        [Required]
        public string BanReason { get; set; } = string.Empty;

        /// <summary>
        /// Дата бана
        /// </summary>
        [Column("BannedAt")]
        [Required]
        public DateTime BannedAt { get; set; }

        /// <summary>
        /// Признак активности бана
        /// </summary>
        [Column("IsActive")]
        [Required]
        public bool IsActive { get; set; }

        [Timestamp]
        [Column("Version")]
        public byte[] Version { get; set; } = [];
    }
}
