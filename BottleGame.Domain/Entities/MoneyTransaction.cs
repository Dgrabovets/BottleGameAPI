using BottleGame.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Money transaction
    /// </summary>
    [Table("MoneyTransactions")]
    public class MoneyTransaction
    {
        #region Primary Key

        /// <summary>
        /// Transaction identifier
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        #endregion Primary Key

        #region Foreign Keys

        /// <summary>
        /// Player identifier
        /// </summary>
        [Required]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Transaction status identifier
        /// </summary>
        [Required]
        [ForeignKey("TransactionStatus")]
        [Column("StatusId")]
        public int TransactionStatusId { get; set; }

        /// <summary>
        /// Transaction type identifier
        /// </summary>
        [Required]
        [ForeignKey("TransactionType")]
        [Column("TransactionTypeId")]
        public int TransactionTypeId { get; set; }

        #endregion Foreign Keys

        #region Amount Fields

        /// <summary>
        /// Transaction amount
        /// </summary>
        [Required]
        [Column("Amount")]
        public decimal Amount { get; set; }

        #endregion Amount Fields

        #region Processing Fields

        /// <summary>
        /// UUID of the order/payout in the payment processing
        /// </summary>
        [Column("ProcessingOrderId")]
        [MaxLength(100)]
        public string? ProcessingOrderId { get; set; }

        /// <summary>
        /// Payment URL (for deposits)
        /// </summary>
        [Column("PaymentUrl")]
        [MaxLength(500)]
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// Payment gateway code
        /// </summary>
        [Column("PaymentGateway")]
        [MaxLength(50)]
        public string? PaymentGateway { get; set; }

        /// <summary>
        /// Transaction currency code
        /// </summary>
        [Column("Currency")]
        [MaxLength(10)]
        public string? Currency { get; set; }

        /// <summary>
        /// Exchange rate at the time of the transaction
        /// </summary>
        [Column("ExchangeRate")]
        public decimal? ExchangeRate { get; set; }

        /// <summary>
        /// Whether the processing handled the transaction successfully
        /// </summary>
        [Column("IsProcessingSuccess")]
        public bool? IsProcessingSuccess { get; set; }

        /// <summary>
        /// Error message from the processing
        /// </summary>
        [Column("ProcessingError")]
        [MaxLength(500)]
        public string? ProcessingError { get; set; }

        #endregion Processing Fields

        #region Payout Fields

        /// <summary>
        /// State of the payout at the payment provider (for withdrawals in Live mode)
        /// </summary>
        [Required]
        [Column("PayoutState")]
        public PayoutState PayoutState { get; set; }

        /// <summary>
        /// Time of the first payout request; repeats do not change it
        /// </summary>
        [Column("PayoutRequestedAt")]
        public DateTime? PayoutRequestedAt { get; set; }

        /// <summary>
        /// Wallet sent with the first payout request
        /// </summary>
        [Column("PayoutWallet")]
        [MaxLength(100)]
        public string? PayoutWallet { get; set; }

        #endregion Payout Fields

        #region Date Fields

        /// <summary>
        /// Transaction creation date
        /// </summary>
        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Transaction processing date
        /// </summary>
        [Column("ProcessedAt")]
        public DateTime? ProcessedAt { get; set; }

        #endregion Date Fields

        #region Concurrency

        /// <summary>
        /// Row version for optimistic concurrency. In PostgreSQL this is the xmin system column,
        /// so no separate column or migration is needed
        /// </summary>
        public uint Version { get; set; }

        #endregion Concurrency

        #region Navigation Properties

        /// <summary>
        /// Related player
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        /// <summary>
        /// Transaction type
        /// </summary>
        public virtual TransactionType TransactionType { get; set; } = null!;

        /// <summary>
        /// Transaction status
        /// </summary>
        public virtual TransactionStatus TransactionStatus { get; set; } = null!;

        #endregion Navigation Properties
    }
}
