using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Данные о типе транзакции
    /// </summary>
    [Table("TransactionType")]
    public class TransactionType
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        [Required]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        // Навигационное свойство
        public virtual ICollection<MoneyTransaction> MoneyTransactions { get; set; } = null!;
    }
}
