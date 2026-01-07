namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Статистика игрока
    /// </summary>
    public class PlayerStatisticsResponse
    {
        /// <summary>
        /// Данные игрока
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Дата бана (если забанен)
        /// </summary>
        public DateTime? BannedAt { get; set; }

        /// <summary>
        /// Текущий баланс
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Количество сыгранных раундов
        /// </summary>
        public int RoundsQty { get; set; }

        /// <summary>
        /// Количество выигранных раундов
        /// </summary>
        public int WinsQty { get; set; }

        /// <summary>
        /// Количество проигранных раундов
        /// </summary>
        public int LossQty { get; set; }

        /// <summary>
        /// Сумма выигрышей
        /// </summary>
        public decimal WinAmount { get; set; }

        /// <summary>
        /// Сумма проигрышей
        /// </summary>
        public decimal LossAmount { get; set; }
    }
}
