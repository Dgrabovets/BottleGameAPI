namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// DTO для статуса или типа транзакции
    /// </summary>
    public class TransactionStatusOrTypeDTO
    {
        /// <summary>
        /// Уникальный идентификатор
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
