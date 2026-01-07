namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// DTO транзакции
    /// </summary>
    public class TransactionDTO
    {
        /// <summary>
        /// Уникальный идентификатор транзакции
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Сумма транзакции
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// ID статуса транзакции
        /// </summary>
        public int StatusId { get; set; }

        /// <summary>
        /// Название статуса транзакции
        /// </summary>
        public string? StatusName { get; set; }

        /// <summary>
        /// ID типа транзакции
        /// </summary>
        public int TypeId { get; set; }

        /// <summary>
        /// Название типа транзакции
        /// </summary>
        public string? TypeName { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата обработки
        /// </summary>
        public DateTime? ProcessedAt { get; set; }
    }
}
