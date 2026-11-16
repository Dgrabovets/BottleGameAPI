using System.Net;
using System.Text.Json;

namespace BottleGame.API.Middleware
{
    /// <summary>
    /// Middleware для глобальной обработки исключений в приложении
    /// </summary>
    /// <remarks>
    /// Перехватывает все необработанные исключения и возвращает клиенту 
    /// унифицированный JSON-ответ с информацией об ошибке.
    /// Также выполняет централизованное логирование всех ошибок.
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="GlobalExceptionMiddleware"/>
    /// </remarks>
    /// <param name="next">Делегат следующего middleware в pipeline</param>
    /// <param name="logger">Логгер для записи ошибок</param>
    /// <param name="environment">Окружение приложения (в Production детали исключений не отдаются клиенту)</param>
    public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
    {

        /// <summary>
        /// Делегат для вызова следующего middleware в pipeline
        /// </summary>
        private readonly RequestDelegate _next = next;

        /// <summary>
        /// Логгер для записи информации об ошибках
        /// </summary>
        private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;

        private readonly IWebHostEnvironment _environment = environment;



        /// <summary>
        /// Обрабатывает HTTP-запрос и перехватывает исключения
        /// </summary>
        /// <param name="context">HTTP контекст текущего запроса</param>
        /// <returns>Асинхронная задача обработки запроса</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }



        /// <summary>
        /// Обрабатывает исключение и формирует ответ клиенту
        /// </summary>
        /// <param name="context">HTTP контекст текущего запроса</param>
        /// <param name="exception">Перехваченное исключение</param>
        /// <returns>Асинхронная задача отправки ответа</returns>
        /// <remarks>
        /// Маппинг исключений на HTTP статус-коды:
        /// <list type="bullet">
        ///     <item><description><see cref="KeyNotFoundException"/> → 404 Not Found</description></item>
        ///     <item><description><see cref="UnauthorizedAccessException"/> → 401 Unauthorized</description></item>
        ///     <item><description><see cref="ArgumentException"/> → 400 Bad Request</description></item>
        ///     <item><description><see cref="InvalidOperationException"/> → 400 Bad Request</description></item>
        ///     <item><description>Остальные исключения → 500 Internal Server Error</description></item>
        /// </list>
        /// </remarks>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse();
            var isProduction = _environment.IsProduction();

            switch (exception)
            {
                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = exception.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning(exception, "Ресурс не найден: {Message}", exception.Message);
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = $"Доступ запрещён\r\n{exception.Message}";
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning(exception, "Неавторизованный доступ: {Message}", exception.Message);
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = exception.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning(exception, "Некорректный запрос: {Message}", exception.Message);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = $"Внутренняя ошибка сервера\r\n{exception.Message}";
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogError(exception, "Необработанное исключение: {Type}: {Message}. StackTrace: {StackTrace}",
                        exception.GetType().Name, exception.Message, exception.StackTrace);
                    break;
            }

            var result = JsonSerializer.Serialize(errorResponse, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(result);
        }

    }
}
