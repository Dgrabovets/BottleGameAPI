using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Ban history of the player
    /// </summary>
    [Table("BansHistory")]
    public class BanHistoryEntity
    {
        /// <summary>
        /// Record identifier
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
        /// Unban date
        /// </summary>
        [Column("UnbannedAt")]
        public DateTime? UnbannedAt { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Related player
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
