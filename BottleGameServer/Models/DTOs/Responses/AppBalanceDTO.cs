using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.DTOs.Responses
{
    /// <summary>
    /// Модель ответа на запрос получения баланса приложения
    /// </summary>
    public class AppBalanceDTO
    {
        /// <summary>
        /// Игровой баланс игрока
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// Время пополнения баланса приложения
        /// </summary>
        public DateTime? BalanceAmountUpdatedAt { get; set; }

        /// <summary>
        /// Кем был последний раз пополнен баланс - ИД
        /// </summary>
        public int? BalanceAmountAdminIdUpdatedBy { get; set; }

        /// <summary>
        /// Кем был последний раз пополнен баланс - логин
        /// </summary>
        public string BalanceAmountAdminLoginUpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Нижний порог для баланса (НЗ)
        /// </summary>
        public int LowerThreshold { get; set; }

        /// <summary>
        /// Время обновления нижнего порога баланса (НЗ)
        /// </summary>
        public DateTime? LowerThresholdUpdatedAt { get; set; }

        /// <summary>
        /// Кем был обновлен нижний порог - ИД
        /// </summary>
        public int? LowerThresholdAdminIdUpdatedBy { get; set; }

        /// <summary>
        /// Кем был обновлен нижний порог - логин
        /// </summary>
        public string LowerThresholdAdminLoginUpdatedBy { get; set; } = string.Empty;
    }
}
