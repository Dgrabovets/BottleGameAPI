using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса на пополнение баланса приложения
    /// </summary>
    public class TopUpAppBalanceRequest
    {
        /// <summary>
        /// Сумма пополнения
        /// </summary>
        [Required]
        public int Amount { get; set; }
    }
}
