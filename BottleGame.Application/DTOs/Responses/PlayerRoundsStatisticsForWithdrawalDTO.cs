namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Статистика раундов игрока для вывода
    /// </summary>
    public class PlayerRoundsStatisticsForWithdrawalDTO
    {
        /// <summary>
        /// Данные игрока
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Последний депозит
        /// </summary>
        public TransactionDTO? LastDeposit { get; set; }

        /// <summary>
        /// Общее количество сыгранных раундов
        /// </summary>
        public int TotalRoundsPlayed { get; set; }
    }
}
