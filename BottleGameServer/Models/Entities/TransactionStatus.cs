using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.Entities
{
    [Table("TransactionStatuses")]
    public class TransactionStatus
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
