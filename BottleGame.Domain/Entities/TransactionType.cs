using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Тип транзакции
    /// </summary>
    [Table("TransactionTypes")]
    public class TransactionType
    {
        /// <summary>
        /// Идентификатор типа
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// Название типа
        /// </summary>
        [Required]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        #region Navigation Properties

        /// <summary>
        /// Связанные транзакции
        /// </summary>
        public virtual ICollection<MoneyTransaction> MoneyTransactions { get; set; } = new List<MoneyTransaction>();

        #endregion Navigation Properties
    }
}
