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
        /// Общая сумма депозитов
        /// </summary>
        public decimal DepositsTotal { get; set; }

        /// <summary>
        /// Общая сумма балансов игроков
        /// </summary>
        public decimal BalancesTotal { get; set; }

        /// <summary>
        /// Общая сумма ожидающих выводов
        /// </summary>
        public decimal PendingWithdrawalsTotal { get; set; }
    }
}
