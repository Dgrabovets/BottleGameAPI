using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Сущность бана игрока
    /// </summary>
    [Table("Bans")]
    public class Ban
    {
        /// <summary>
        /// Идентификатор игрока (первичный ключ)
        /// </summary>
        [Key]
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
        /// Активен ли бан
        /// </summary>
        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Связанный игрок
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
