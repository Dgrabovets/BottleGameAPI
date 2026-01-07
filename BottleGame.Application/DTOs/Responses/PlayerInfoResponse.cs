namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Информация об игроке
    /// </summary>
    public class PlayerInfoResponse
    {
        /// <summary>
        /// Данные игрока
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Баланс игрока
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Раунды игрока
        /// </summary>
        public IEnumerable<RoundDTO> Rounds { get; set; } = [];

        /// <summary>
        /// Дата бана (если забанен)
        /// </summary>
        public DateTime? BannedAt { get; set; }

        /// <summary>
        /// ID приглашённых игроков
        /// </summary>
        public IEnumerable<int> PlayersIdsInvited { get; set; } = null!;
    }
}
