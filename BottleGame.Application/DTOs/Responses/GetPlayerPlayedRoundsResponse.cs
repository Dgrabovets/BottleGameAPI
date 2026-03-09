namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Ответ со списком сыгранных раундов игрока
    /// </summary>
    public class GetPlayerPlayedRoundsResponse
    {
        /// <summary>
        /// Данные игрока
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Список раундов
        /// </summary>
        public List<RoundDTO> Rounds { get; set; } = [];
    }
}
