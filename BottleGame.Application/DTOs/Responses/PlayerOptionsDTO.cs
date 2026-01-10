namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// DTO настроек игрока
    /// </summary>
    public class PlayerOptionsDTO
    {
        /// <summary>
        /// Персональный шанс выигрыша
        /// </summary>
        public float WinChance { get; set; }

        /// <summary>
        /// Кошелёк для выплат
        /// </summary>
        public string? Wallet { get; set; }

        /// <summary>
        /// Валюта (строка)
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Метод выплаты (строка)
        /// </summary>
        public string PayoutMethod { get; set; } = string.Empty;

        /// <summary>
        /// Метод оплаты (строка)
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Тип платёжных реквизитов (например, "card", "phone", "sbp")
        /// </summary>
        public string PaymentDetailType { get; set; } = string.Empty;
    }
}
