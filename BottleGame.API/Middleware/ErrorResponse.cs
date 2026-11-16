using System.Net;
using System.Text.Json;

namespace BottleGame.API.Middleware
{
    /// <summary>
    /// Модель ответа об ошибке для клиента
    /// </summary>
    /// <remarks>
    /// Унифицированная модель, которая возвращается клиенту при возникновении любой ошибки.
    /// Содержит HTTP статус-код, сообщение об ошибке и временную метку.
    /// </remarks>
    public class ErrorResponse
    {

        /// <summary>
        /// HTTP статус-код ошибки
        /// </summary>
        /// <example>404</example>
        public int StatusCode { get; set; }

        /// <summary>
        /// Человекочитаемое сообщение об ошибке
        /// </summary>
        /// <example>Ресурс не найден</example>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Временная метка возникновения ошибки в формате UTC
        /// </summary>
        /// <example>2025-12-24T10:30:00Z</example>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
