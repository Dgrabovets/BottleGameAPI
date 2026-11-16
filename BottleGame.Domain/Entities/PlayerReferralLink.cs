using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Referral link of the player
    /// </summary>
    [Table("ReferralLinks")]
    public class ReferralLink
    {
        /// <summary>
        /// Player identifier (primary key)
        /// </summary>
        [Key]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Referral link
        /// </summary>
        [Required]
        [Column("Link")]
        public string Link { get; set; } = string.Empty;

        #region Navigation Properties

        /// <summary>
        /// Related player
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
