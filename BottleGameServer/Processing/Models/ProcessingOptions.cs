namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Опции конфигурации для взаимодействия с процессинговой площадкой.
    /// </summary>
    public class ProcessingOptions
    {
        /// <summary>
        /// Базовый URL API процессинговой площадки.
        /// </summary>
        public string ApiUrl { get; set; } = string.Empty;

        /// <summary>
        /// Секретный ключ для аутентификации запросов к процессинговой площадке.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// ID мерчанта (торговца) в системе процессинговой площадки.
        /// </summary>
        public string MerchantId { get; set; } = string.Empty;
    }
}