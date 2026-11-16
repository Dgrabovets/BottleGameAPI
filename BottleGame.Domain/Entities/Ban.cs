using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Player ban entity
    /// </summary>
    [Table("Bans")]
    public class Ban
    {
        /// <summary>
        /// Player identifier (primary key)
        /// </summary>
        [Key]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Reason for the ban
        /// </summary>
        [Required]
        [Column("BanReason")]
        public string BanReason { get; set; } = string.Empty;

        /// <summary>
        /// Ban date
        /// </summary>
        [Required]
        [Column("BannedAt")]
        public DateTime BannedAt { get; set; }

        /// <summary>
        /// Whether the ban is active
        /// </summary>
        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Related player
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
