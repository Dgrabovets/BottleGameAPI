namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса на изменение игрока
    /// </summary>
    public class EditPlayerRequest
    {
        /// <summary>
        /// Имя
        /// </summary>
        public string? Name { get; set; } = string.Empty;

        /// <summary>
        /// Ссылка на аватарку
        /// </summary>
        public string? AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Шанс выигрыша для игрока
        /// </summary>
        public float? WinChance { get; set; }

        /// <summary>
        /// Предпочитаемая валюта игрока (RUB, USD, KZT)
        /// </summary>
        public string? Currency { get; set; } = string.Empty;

        /// <summary>
        /// Кошелёк или номер карты игрока для вывода средств
        /// </summary>
        public string? Wallet { get; set; } = string.Empty;

        /// <summary>
        /// Предпочитаемый метод выплаты игрока
        /// </summary>
        public string? PayoutMethod { get; set; } = string.Empty;

        /// <summary>
        /// Предпочитаемый метод платежа игрока
        /// </summary>
        public string? PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Новый баланс игрока (добавлен для удобного тестирование)
        /// </summary>
        public int? Balance { get; set; }
    }
}
