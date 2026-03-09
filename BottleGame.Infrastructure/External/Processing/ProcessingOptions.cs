using System.ComponentModel.DataAnnotations;

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
        /// Передаётся в заголовке Access-Token. Задаётся через Processing__AccessToken.
        /// </summary>
        [Required(ErrorMessage = "Processing:AccessToken обязателен. Задайте переменную окружения Processing__AccessToken или секрет PAYSHARK_ACCESS_TOKEN.")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// UUID мерчанта в платёжной системе. Задаётся через Processing__MerchantId.
        /// </summary>
        [Required(ErrorMessage = "Processing:MerchantId обязателен. Задайте переменную окружения Processing__MerchantId или секрет PAYSHARK_MERCHANT_ID.")]
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Базовый URL для callback-уведомлений от платёжной системы.
        /// </summary>
        /// <example>https://your-api.com/api/webhook</example>
        [Required(ErrorMessage = "Processing:CallbackUrl обязателен. Задайте переменную окружения Processing__CallbackUrl.")]
        public string CallbackUrl { get; set; } = string.Empty;

        /// <summary>
        /// Максимальное время ожидания ответа от API в миллисекундах.
        /// Используется в заголовке X-Max-Wait-Ms.
        /// </summary>
        /// <remarks>По умолчанию: 30000 (30 секунд).</remarks>
        public int MaxWaitMs { get; set; } = 30000;
    }
}
