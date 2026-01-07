using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Реферальная ссылка игрока
    /// </summary>
    [Table("ReferralLinks")]
    public class ReferralLink
    {
        /// <summary>
        /// Идентификатор игрока (первичный ключ)
        /// </summary>
        [Key]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Реферальная ссылка
        /// </summary>
        [Required]
        [Column("Link")]
        public string Link { get; set; } = string.Empty;

        #region Navigation Properties

        /// <summary>
        /// Связанный игрок
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
