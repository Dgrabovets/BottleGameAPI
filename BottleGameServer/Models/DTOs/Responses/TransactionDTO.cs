namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Дочерняя модель ответа плучения всех транзакций пользователя, содержащая данные о транзакции
    /// </summary>
    public class TransactionDTO
    {
        /// <summary>
        /// ИД
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Сумма
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Статус ИД
        /// </summary>
        public int StatusId { get; set; }

        /// <summary>
        /// Имя статуса
        /// </summary>
        public string? StatusName { get; set; } = string.Empty;

        /// <summary>
        /// Тип ИД
        /// </summary>
        public int TypeId { get; set; }

        /// <summary>
        /// Имя типа
        /// </summary>
        public string? TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата обработки
        /// </summary>
        public DateTime ProcessedAt { get; set; }
    }
}
