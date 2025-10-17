using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Модель для хранения raw запросов от процессинговой площадки NicePay
    /// </summary>
    [Table("NicePayWebhookLogs")]
    public class NicePayWebhookLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Полные raw данные запроса (JSON для POST или query string для GET)
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string RawRequest { get; set; } = string.Empty;

        /// <summary>
        /// Тип запроса (GET/POST)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string HttpMethod { get; set; } = string.Empty;

        /// <summary>
        /// Тип контента
        /// </summary>
        [MaxLength(100)]
        public string? ContentType { get; set; }

        /// <summary>
        /// IP адрес отправителя
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Статус проверки подписи
        /// </summary>
        [Required]
        public bool IsSignatureValid { get; set; }

        /// <summary>
        /// Статус обработки (Success, Error, InvalidSignature, etc.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ProcessingStatus { get; set; } = string.Empty;

        /// <summary>
        /// Сообщение об ошибке (если была)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// ID связанной транзакции в нашей системе (если удалось распарсить order_id)
        /// </summary>
        public int? TransactionId { get; set; }

        /// <summary>
        /// Тип webhook (определяется из result)
        /// </summary>
        [MaxLength(20)]
        public string? WebhookType { get; set; }

        /// <summary>
        /// Результат операции (success, error, success_payout, error_payout)
        /// </summary>
        [MaxLength(50)]
        public string? Result { get; set; }

        /// <summary>
        /// Order ID из запроса
        /// </summary>
        [MaxLength(100)]
        public string? OrderId { get; set; }

        /// <summary>
        /// Время получения запроса
        /// </summary>
        [Required]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Время завершения обработки
        /// </summary>
        public DateTime? ProcessedAt { get; set; }
    }
}
