using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Данные о денежной транзакции
    /// </summary>
    [Table("MoneyTransactions")]
    public class MoneyTransaction
    {
        /// <summary>
        /// Идентификатор транзакции
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор игрока
        /// </summary>
        [ForeignKey("Player")]
        [Column("PlayerId")]
        [Required]
        public int PlayerId { get; set; }
        public virtual Player Player { get; set; } = null!;

        /// <summary>
        /// Сумма транзакции
        /// </summary>
        [Required]
        [Column("Amount")]
        public int Amount { get; set; }

        /// <summary>
        /// Идентификатор статуса
        /// </summary>
        [ForeignKey("Status")]
        [Column("StatusId")]
        [Required]
        public int TransactionStatusId { get; set; }
        public virtual TransactionStatus TransactionStatus { get; set; } = null!;

        /// <summary>
        /// Идентификатор типа транзакции
        /// </summary>
        [ForeignKey("TransactionType")]
        [Column("TransactionTypeId")]
        [Required]
        public int TransactionTypeId { get; set; }
        public virtual TransactionType TransactionType { get; set; } = null!;

        /// <summary>
        /// Дата создания
        /// </summary>
        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата обработки
        /// </summary>
        [Column("ProcessedAt")]
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// Признак успешной обработки процессинга
        /// </summary>
        [Column("IsProcessingSuccess")]
        public bool? IsProcessingSuccess { get; set; }
    }
}
