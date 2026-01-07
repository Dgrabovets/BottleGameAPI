using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Денежная транзакция.
    /// </summary>
    [Table("MoneyTransactions")]
    public class MoneyTransaction
    {
        #region Primary Key

        /// <summary>
        /// Идентификатор транзакции.
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        #endregion Primary Key

        #region Foreign Keys

        /// <summary>
        /// Идентификатор игрока.
        /// </summary>
        [Required]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Идентификатор статуса транзакции.
        /// </summary>
        [Required]
        [ForeignKey("TransactionStatus")]
        [Column("StatusId")]
        public int TransactionStatusId { get; set; }

        /// <summary>
        /// Идентификатор типа транзакции.
        /// </summary>
        [Required]
        [ForeignKey("TransactionType")]
        [Column("TransactionTypeId")]
        public int TransactionTypeId { get; set; }

        #endregion Foreign Keys

        #region Amount Fields

        /// <summary>
        /// Сумма транзакции.
        /// </summary>
        [Required]
        [Column("Amount")]
        public decimal Amount { get; set; }

        #endregion Amount Fields

        #region Processing Fields

        /// <summary>
        /// UUID ордера/выплаты в платёжном процессинге.
        /// </summary>
        [Column("ProcessingOrderId")]
        [MaxLength(100)]
        public string? ProcessingOrderId { get; set; }

        /// <summary>
        /// URL для оплаты (для депозитов).
        /// </summary>
        [Column("PaymentUrl")]
        [MaxLength(500)]
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// Код платёжного шлюза.
        /// </summary>
        [Column("PaymentGateway")]
        [MaxLength(50)]
        public string? PaymentGateway { get; set; }

        /// <summary>
        /// Код валюты транзакции.
        /// </summary>
        [Column("Currency")]
        [MaxLength(10)]
        public string? Currency { get; set; }

        /// <summary>
        /// Курс обмена на момент транзакции.
        /// </summary>
        [Column("ExchangeRate")]
        public decimal? ExchangeRate { get; set; }

        /// <summary>
        /// Успешно ли обработана транзакция процессингом.
        /// </summary>
        [Column("IsProcessingSuccess")]
        public bool? IsProcessingSuccess { get; set; }

        /// <summary>
        /// Сообщение об ошибке от процессинга.
        /// </summary>
        [Column("ProcessingError")]
        [MaxLength(500)]
        public string? ProcessingError { get; set; }

        #endregion Processing Fields

        #region Date Fields

        /// <summary>
        /// Дата создания транзакции.
        /// </summary>
        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата обработки транзакции.
        /// </summary>
        [Column("ProcessedAt")]
        public DateTime? ProcessedAt { get; set; }

        #endregion Date Fields

        #region Navigation Properties

        /// <summary>
        /// Связанный игрок.
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        /// <summary>
        /// Тип транзакции.
        /// </summary>
        public virtual TransactionType TransactionType { get; set; } = null!;

        /// <summary>
        /// Статус транзакции.
        /// </summary>
        public virtual TransactionStatus TransactionStatus { get; set; } = null!;

        #endregion Navigation Properties
    }
}
