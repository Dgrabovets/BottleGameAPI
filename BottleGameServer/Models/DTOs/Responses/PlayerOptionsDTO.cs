using BottleGameServer.Models.Entities;

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

        /// <summary>
        /// Предпочитаемая валюта игрока (RUB, USD, KZT)
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Кошелёк или номер карты игрока для вывода средств
        /// </summary>
        public string Wallet { get; set; } = string.Empty;

        /// <summary>
        /// Предпочитаемый метод выплаты игрока
        /// </summary>
        public string PayoutMethod { get; set; } = string.Empty;

        /// <summary>
        /// Предпочитаемый способ оплаты игрока
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
