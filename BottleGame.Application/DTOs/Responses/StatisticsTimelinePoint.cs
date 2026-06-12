namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Точка временной шкалы статистики
    /// </summary>
    public class StatisticsTimelinePoint
    {
        /// <summary>
        /// Начало бакета
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Накопительное количество игроков на конец бакета
        /// </summary>
        public int UsersCount { get; set; }

        /// <summary>
        /// Сумма пополнений за бакет
        /// </summary>
        public decimal DepositsAmount { get; set; }

        /// <summary>
        /// Сумма одобренных выводов за бакет
        /// </summary>
        public decimal WithdrawalsAmount { get; set; }

        /// <summary>
        /// Доход за бакет (пополнения − выводы)
        /// </summary>
        public decimal IncomeAmount { get; set; }
    }
}
