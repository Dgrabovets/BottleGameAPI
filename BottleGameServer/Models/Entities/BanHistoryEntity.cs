using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Запись бана игрока в истории
    /// </summary>
    [Table("BansHistory")]
    public class BanHistoryEntity
    {
        [Key]
        [Column("Id")]
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
        /// Дата разбана
        /// </summary>
        [Column("UnbannedAt")]
        public DateTime? UnbannedAt { get; set; }

        [Timestamp]
        [Column("Version")]
        public byte[] Version { get; set; } = [];
    }
}
