using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Player balance
    /// </summary>
    [Table("PlayersBalance")]
    public class Balance
    {
        /// <summary>
        /// Player identifier (primary key)
        /// </summary>
        [Key]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Amount on the balance
        /// </summary>
        [Required]
        [Column("Amount")]
        public decimal Amount { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Related player
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
