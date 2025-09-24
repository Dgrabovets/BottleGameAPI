using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса на бан игрока
    /// </summary>
    public class BanPlayerRequest
    {
        /// <summary>
        /// ИД игрока
        /// </summary>
        [Required]
        public int PlayerId { get; set; }

        /// <summary>
        /// Причина бана
        /// </summary>
        [MaxLength(255)]
        [Required]
        public string BanReason { get; set; } = string.Empty;
    }
}
