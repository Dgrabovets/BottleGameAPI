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

        #region Concurrency

        /// <summary>
        /// Row version for optimistic concurrency. In PostgreSQL this is the xmin system column,
        /// so no separate column or migration is needed. Two requests that change the same balance at once
        /// cannot both win: the second one fails instead of overwriting the first
        /// </summary>
        public uint Version { get; set; }

        #endregion Concurrency

        #region Navigation Properties

        /// <summary>
        /// Related player
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
