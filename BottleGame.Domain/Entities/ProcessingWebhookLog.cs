using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Лог вебхуков от платёжного процессинга.
    /// </summary>
    [Table("ProcessingWebhookLogs")]
    public class ProcessingWebhookLog
    {
        /// <summary>
        /// Идентификатор записи.
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Сырой запрос (JSON body).
        /// </summary>
        [Required]
        [Column("RawRequest")]
        public string RawRequest { get; set; } = string.Empty;

        /// <summary>
        /// HTTP метод запроса.
        /// </summary>
        [Required]
        [Column("HttpMethod")]
        [MaxLength(10)]
        public string HttpMethod { get; set; } = string.Empty;

        /// <summary>
        /// Content-Type запроса.
        /// </summary>
        [Column("ContentType")]
        [MaxLength(100)]
        public string? ContentType { get; set; }

        /// <summary>
        /// IP-адрес клиента.
        /// </summary>
        [Column("IpAddress")]
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User-Agent клиента.
        /// </summary>
        [Column("UserAgent")]
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Валидна ли подпись (если применимо).
        /// </summary>
        [Required]
        [Column("IsSignatureValid")]
        public bool IsSignatureValid { get; set; }

        /// <summary>
        /// Статус обработки вебхука (Processing, Success, Error).
        /// </summary>
        [Required]
        [Column("ProcessingStatus")]
        [MaxLength(50)]
        public string ProcessingStatus { get; set; } = string.Empty;

        /// <summary>
        /// Сообщение об ошибке (если есть).
        /// </summary>
        [Column("ErrorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Внешний идентификатор транзакции (ExternalId).
        /// </summary>
        [Column("TransactionId")]
        [MaxLength(100)]
        public string? TransactionId { get; set; }

        /// <summary>
        /// Тип вебхука (order-status, payout-status).
        /// </summary>
        [Column("WebhookType")]
        [MaxLength(50)]
        public string? WebhookType { get; set; }

        /// <summary>
        /// Результат обработки (status/sub_status).
        /// </summary>
        [Column("Result")]
        [MaxLength(100)]
        public string? Result { get; set; }

        /// <summary>
        /// UUID ордера/выплаты в процессинге.
        /// </summary>
        [Column("OrderId")]
        [MaxLength(100)]
        public string? OrderId { get; set; }

        /// <summary>
        /// UUID мерчанта.
        /// </summary>
        [Column("MerchantId")]
        [MaxLength(100)]
        public string? MerchantId { get; set; }

        /// <summary>
        /// Дата получения вебхука.
        /// </summary>
        [Required]
        [Column("ReceivedAt")]
        public DateTime ReceivedAt { get; set; }

        /// <summary>
        /// Дата обработки вебхука.
        /// </summary>
        [Column("ProcessedAt")]
        public DateTime? ProcessedAt { get; set; }
    }
}


