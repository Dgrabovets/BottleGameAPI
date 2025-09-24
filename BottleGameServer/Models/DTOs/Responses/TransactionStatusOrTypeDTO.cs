namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа статуса или типа транзакции
    /// </summary>
    public class TransactionStatusOrTypeDTO
    {
        /// <summary>
        /// ИД
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
