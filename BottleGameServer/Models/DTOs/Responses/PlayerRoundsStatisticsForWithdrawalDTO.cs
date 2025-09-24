namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа, содержащая статистику по раундам игрока для вывода средств
    /// </summary>
    public class PlayerRoundsStatisticsForWithdrawalDTO
    {
        /// <summary>
        /// Игрок
        /// </summary>
        public PlayerDTO Player { get; set; } = new PlayerDTO();

        /// <summary>
        /// Последнее пополнение
        /// </summary>
        public TransactionDTO? LastDeposit { get; set; } = new TransactionDTO();

        /// <summary>
        /// Количество сыгранных раундов
        /// </summary>
        public int TotalRoundsPlayed { get; set; }
    }
}
