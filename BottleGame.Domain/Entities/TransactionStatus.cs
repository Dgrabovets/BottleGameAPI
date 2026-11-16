using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Transaction status
    /// </summary>
    [Table("TransactionStatuses")]
    public class TransactionStatus
    {
        /// <summary>
        /// Status identifier
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// Status name
        /// </summary>
        [Required]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        #region Navigation Properties

        /// <summary>
        /// Related transactions
        /// </summary>
        public virtual ICollection<MoneyTransaction> MoneyTransactions { get; set; } = new List<MoneyTransaction>();

        #endregion Navigation Properties
    }
}
