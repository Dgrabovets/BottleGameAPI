namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа, содержащая данные о статистике игрока
    /// </summary>
    public class PlayerStatisticsResponse
    {
        /// <summary>
        /// Данные об игроке
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Данные о балансе игрока
        /// </summary>
        public int Balance { get; set; }

        /// <summary>
        /// Кол-во сыгранных раундов
        /// </summary>
        public int RoundsQty { get; set; }

        /// <summary>
        /// Кол-во побед
        /// </summary>
        public int WinsQty { get; set; }

        /// <summary>
        /// Кол-во поражений
        /// </summary>
        public int LossQty { get; set; }

        /// <summary>
        /// Размер выигрыша
        /// </summary>
        public int WinAmount { get; set; }

        /// <summary>
        /// Размер проигрыша
        /// </summary>
        public int LossAmount { get; set; }

        /// <summary>
        /// Дата бана
        /// </summary>
        public DateTime? BannedAt { get; set; }
    }
}
