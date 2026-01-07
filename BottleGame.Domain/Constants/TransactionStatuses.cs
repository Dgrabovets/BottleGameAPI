namespace BottleGame.Domain.Constants
{
    /// <summary>
    /// Статусы денежных транзакций
    /// </summary>
    public static class TransactionStatuses
    {
        /// <summary>
        /// Ожидает обработки
        /// </summary>
        public const int Pending = 1;

        /// <summary>
        /// Одобрено / Успешно
        /// </summary>
        public const int Approved = 2;

        /// <summary>
        /// Отклонено / Ошибка
        /// </summary>
        public const int Rejected = 3;

        /// <summary>
        /// Названия статусов для логирования
        /// </summary>
        public static string GetName(int statusId) => statusId switch
        {
            Pending => "Ожидание",
            Approved => "Одобрено",
            Rejected => "Отклонено",
            _ => "Неизвестный статус"
        };
    }
}


