namespace BottleGame.Infrastructure.External.Processing
{
    /// <summary>
    /// Класс конфигурации для взаимодействия с платёжным процессингом.
    /// </summary>
    public class ProcessingOptions
    {
        /// <summary>
        /// Базовый URL API платёжного сервиса.
        /// </summary>
        /// <example>https://app.payshark.net</example>
        public string BaseUrl { get; set; } = "https://app.payshark.net";

        /// <summary>
        /// Access Token для аутентификации запросов к API.
        /// Передаётся в заголовке Access-Token.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// UUID мерчанта в платёжной системе.
        /// </summary>
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Базовый URL для callback-уведомлений от платёжной системы.
        /// </summary>
        /// <example>https://your-api.com/api/webhook</example>
        public string CallbackUrl { get; set; } = string.Empty;

        /// <summary>
        /// Максимальное время ожидания ответа от API в миллисекундах.
        /// Используется в заголовке X-Max-Wait-Ms.
        /// </summary>
        /// <remarks>По умолчанию: 30000 (30 секунд).</remarks>
        public int MaxWaitMs { get; set; } = 30000;
    }
}
