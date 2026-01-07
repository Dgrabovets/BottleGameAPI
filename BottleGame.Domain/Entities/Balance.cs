using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Баланс игрока
    /// </summary>
    [Table("PlayersBalance")]
    public class Balance
    {
        /// <summary>
        /// Идентификатор игрока (первичный ключ)
        /// </summary>
        [Key]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Сумма на балансе
        /// </summary>
        [Required]
        [Column("Amount")]
        public decimal Amount { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Связанный игрок
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
