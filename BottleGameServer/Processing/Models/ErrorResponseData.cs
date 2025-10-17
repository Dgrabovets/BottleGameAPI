namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Модель данных ошибки
    /// </summary>
    public class ErrorResponseData
    {
        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string message { get; set; } = string.Empty;
    }
}