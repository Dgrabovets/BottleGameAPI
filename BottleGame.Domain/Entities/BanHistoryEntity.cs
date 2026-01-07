using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// История банов игрока
    /// </summary>
    [Table("BansHistory")]
    public class BanHistoryEntity
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
        /// Причина бана
        /// </summary>
        [Required]
        [Column("BanReason")]
        public string BanReason { get; set; } = string.Empty;

        /// <summary>
        /// Дата бана
        /// </summary>
        [Required]
        [Column("BannedAt")]
        public DateTime BannedAt { get; set; }

        /// <summary>
        /// Дата разбана
        /// </summary>
        [Column("UnbannedAt")]
        public DateTime? UnbannedAt { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Связанный игрок
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
