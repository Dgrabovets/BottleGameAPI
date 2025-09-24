using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса для завершения раунда
    /// </summary>
    public class FinishRoundRequest
    {
        /// <summary>
        /// Ставка игрока
        /// </summary>
        [Required(ErrorMessage = "Ставка обязательна")]
        [Range(1, int.MaxValue, ErrorMessage = "Ставка должна быть положительной")]
        public int Bet { get; set; }

        /// <summary>
        /// Признак, выиграл ли игрок
        /// </summary>
        [Required]
        public bool IsWin { get; set; }
    }
}
