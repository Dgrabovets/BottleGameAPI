namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на редактирование игрока
    /// </summary>
    public class EditPlayerRequest
    {
        /// <summary>
        /// Имя игрока
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// URL аватара игрока
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Персональный шанс выигрыша
        /// </summary>
        public float? WinChance { get; set; }

        /// <summary>
        /// Валюта
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Метод оплаты
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Метод выплаты
        /// </summary>
        public string? PayoutMethod { get; set; }

        /// <summary>
        /// Кошелёк для выплат
        /// </summary>
        public string? Wallet { get; set; }

        /// <summary>
        /// Баланс (только для админа)
        /// </summary>
        public decimal? Balance { get; set; }
    }
}
