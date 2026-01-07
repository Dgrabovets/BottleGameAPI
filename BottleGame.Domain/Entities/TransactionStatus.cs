using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Статус транзакции
    /// </summary>
    [Table("TransactionStatuses")]
    public class TransactionStatus
    {
        /// <summary>
        /// Идентификатор статуса
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// Название статуса
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
