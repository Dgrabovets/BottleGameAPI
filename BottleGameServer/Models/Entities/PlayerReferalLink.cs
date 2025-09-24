using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Данные о реферальной ссылке пользователя
    /// </summary>
    [Table("ReferalLinks")]
    public class ReferalLink
    {
        /// <summary>
        /// Идентификатор игрока
        /// </summary>
        [ForeignKey("Player")]
        [Column("PlayerId")]
        [Required]
        public int PlayerId { get; set; }
        public virtual Player Player { get; set; } = null!;

        /// <summary>
        /// Реферальная ссылка
        /// </summary>
        [Column("Link")]
        [Required]
        public string Link { get; set; } = string.Empty;
    }
}
