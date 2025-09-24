using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса на обновление нижнего порога баланса приложения
    /// </summary>
    public class UpdateAppBalanceThresholdRequest
    {
        /// <summary>
        /// Значение нижнего порога баланса
        /// </summary>
        [Required]
        public int LowerThreshold { get; set; }
    }
}
