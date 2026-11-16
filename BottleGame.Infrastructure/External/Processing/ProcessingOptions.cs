namespace BottleGame.Infrastructure.External.Processing
{
    /// <summary>
    /// Настройки платёжного провайдера (ЮKassa).
    /// </summary>
    public class ProcessingOptions
    {
        /// <summary>
        /// Режим работы с платёжным провайдером. Задаётся через Processing__Mode.
        /// </summary>
        /// <remarks>По умолчанию: Simulated. Для реальных платежей укажите Live.</remarks>
        public ProcessingMode Mode { get; set; } = ProcessingMode.Simulated;

        /// <summary>
        /// Базовый URL API ЮKassa.
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.yookassa.ru/v3/";

        /// <summary>
        /// Идентификатор магазина для приёма платежей. Задаётся через Processing__ShopId.
        /// </summary>
        public string ShopId { get; set; } = string.Empty;

        /// <summary>
        /// Секретный ключ магазина. Задаётся через Processing__SecretKey.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор шлюза выплат. Задаётся через Processing__PayoutAgentId; без него выплаты недоступны.
        /// </summary>
        public string PayoutAgentId { get; set; } = string.Empty;

        /// <summary>
        /// Секретный ключ шлюза выплат. Задаётся через Processing__PayoutSecretKey.
        /// </summary>
        public string PayoutSecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Адрес страницы, на которую ЮKassa возвращает игрока после оплаты. Задаётся через Processing__ReturnUrl.
        /// </summary>
        public string ReturnUrl { get; set; } = string.Empty;

        /// <summary>
        /// Максимальное время ожидания ответа от API в секундах.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
