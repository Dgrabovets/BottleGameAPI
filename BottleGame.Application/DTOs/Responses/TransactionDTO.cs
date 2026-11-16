namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Transaction DTO
    /// </summary>
    public class TransactionDTO
    {
        /// <summary>
        /// Unique identifier of the transaction
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Transaction amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Id of the transaction status
        /// </summary>
        public int StatusId { get; set; }

        /// <summary>
        /// Name of the transaction status
        /// </summary>
        public string? StatusName { get; set; }

        /// <summary>
        /// Id of the transaction type
        /// </summary>
        public int TypeId { get; set; }

        /// <summary>
        /// Name of the transaction type
        /// </summary>
        public string? TypeName { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Processing date
        /// </summary>
        public DateTime? ProcessedAt { get; set; }
    }
}
