namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа, содержащая основные данные об игроке
    /// </summary>
    public class PlayerInfoByIdResponse
    {
        /// <summary>
        /// Данные игрока
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Раунды игрока
        /// </summary>
        public IEnumerable<RoundDTO> Rounds { get; set; } = null!;

        /// <summary>
        /// Дата бана
        /// </summary>
        public DateTime? BannedAt { get; set; }

        /// <summary>
        /// Игровой баланс игрока
        /// </summary>
        public int Balance { get; set; }

        /// <summary>
        /// Настройки игры для игрока
        /// </summary>
        public PlayerOptionsDTO Options { get; set; } = null!;

        /// <summary>
        /// Перечисление приглашенных пользователей
        /// </summary>
        public IEnumerable<PlayerDTO> PlayersInvited { get; set; } = null!;
    }
}
