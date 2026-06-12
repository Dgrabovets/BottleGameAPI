namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Статистика для администратора
    /// </summary>
    public class GetAppGeneralStatisticsResponse
    {
        /// <summary>
        /// Общее количество пользователей
        /// </summary>
        public int UsersTotal { get; set; }

        /// <summary>
        /// Сумма успешно обработанных пополнений
        /// </summary>
        public decimal DepositsTotal { get; set; }

        /// <summary>
        /// Общая сумма балансов игроков (только для общей статистики без периода)
        /// </summary>
        public decimal? BalancesTotal { get; set; }

        /// <summary>
        /// Сумма успешно обработанных выводов
        /// </summary>
        public decimal WithdrawalsTotal { get; set; }

        /// <summary>
        /// Доход: успешно обработанные пополнения минус успешно обработанные выводы
        /// </summary>
        public decimal IncomeTotal { get; set; }

        /// <summary>
        /// Сумма выводов в обработке (статус Pending)
        /// </summary>
        public decimal PendingWithdrawalsTotal { get; set; }
    }
}
