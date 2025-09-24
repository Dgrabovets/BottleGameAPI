namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа на запрос получения админской статистики
    /// </summary>
    public class GetAdminStatisticsResponse
    {
        /// <summary>
        /// Общее кол-во пользователей в системе
        /// </summary>
        public int UsersTotal { get; set; }

        /// <summary>
        /// Общая сумма пополнений
        /// </summary>
        public int DepositsTotal { get; set; }

        /// <summary>
        /// Общая сумма балансов пользователей
        /// </summary>
        public int BalancesTotal { get; set; }

        /// <summary>
        /// Общая сумма ожидаемого вывода
        /// </summary>
        public int PendingWithdrawalsTotal { get; set; }
    }
}
