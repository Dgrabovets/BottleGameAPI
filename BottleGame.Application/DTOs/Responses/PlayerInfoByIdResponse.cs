namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Информация об игроке по ID
    /// </summary>
    public class PlayerInfoByIdResponse
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
        /// Настройки игрока
        /// </summary>
        public PlayerOptionsDTO Options { get; set; } = null!;

        /// <summary>
        /// Приглашённые игроки
        /// </summary>
        public IEnumerable<PlayerDTO> PlayersInvited { get; set; } = [];
    }
}
