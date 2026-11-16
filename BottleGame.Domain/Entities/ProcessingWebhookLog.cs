using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Log of webhooks from the payment processing
    /// </summary>
    [Table("ProcessingWebhookLogs")]
    public class ProcessingWebhookLog
    {
        /// <summary>
        /// Record identifier
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Raw request (JSON body)
        /// </summary>
        [Required]
        [Column("RawRequest")]
        public string RawRequest { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method of the request
        /// </summary>
        [Required]
        [Column("HttpMethod")]
        [MaxLength(10)]
        public string HttpMethod { get; set; } = string.Empty;

        /// <summary>
        /// Content-Type of the request
        /// </summary>
        [Column("ContentType")]
        [MaxLength(100)]
        public string? ContentType { get; set; }

        /// <summary>
        /// Client IP address
        /// </summary>
        [Column("IpAddress")]
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Client User-Agent
        /// </summary>
        [Column("UserAgent")]
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Whether the signature is valid (if applicable)
        /// </summary>
        [Required]
        [Column("IsSignatureValid")]
        public bool IsSignatureValid { get; set; }

        /// <summary>
        /// Webhook handling status (Processing, Success, Error)
        /// </summary>
        [Required]
        [Column("ProcessingStatus")]
        [MaxLength(50)]
        public string ProcessingStatus { get; set; } = string.Empty;

        /// <summary>
        /// Error message (if any)
        /// </summary>
        [Column("ErrorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// External transaction identifier (ExternalId)
        /// </summary>
        [Column("TransactionId")]
        [MaxLength(100)]
        public string? TransactionId { get; set; }

        /// <summary>
        /// Webhook type (order-status, payout-status)
        /// </summary>
        [Column("WebhookType")]
        [MaxLength(50)]
        public string? WebhookType { get; set; }

        /// <summary>
        /// Handling result (status/sub_status)
        /// </summary>
        [Column("Result")]
        [MaxLength(100)]
        public string? Result { get; set; }

        /// <summary>
        /// UUID of the order/payout in the processing
        /// </summary>
        [Column("OrderId")]
        [MaxLength(100)]
        public string? OrderId { get; set; }

        /// <summary>
        /// Merchant UUID
        /// </summary>
        [Column("MerchantId")]
        [MaxLength(100)]
        public string? MerchantId { get; set; }

        /// <summary>
        /// Webhook receipt date
        /// </summary>
        [Required]
        [Column("ReceivedAt")]
        public DateTime ReceivedAt { get; set; }

        /// <summary>
        /// Webhook handling date
        /// </summary>
        [Column("ProcessedAt")]
        public DateTime? ProcessedAt { get; set; }
    }
}


