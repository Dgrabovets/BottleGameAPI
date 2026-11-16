namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// DTO for a transaction status or type
    /// </summary>
    public class TransactionStatusOrTypeDTO
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
