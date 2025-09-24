namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель, содержащая информацию о настройках для игрока
    /// </summary>
    public class PlayerOptionsDTO
    {
        /// <summary>
        /// Шанс выигрыша для игрока
        /// </summary>
        public float WinChance { get; set; }
    }
}
