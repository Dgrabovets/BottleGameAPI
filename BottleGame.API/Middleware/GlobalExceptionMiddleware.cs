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
        #region Fields

        /// <summary>
        /// Делегат для вызова следующего middleware в pipeline
        /// </summary>
        private readonly RequestDelegate _next = next;

        /// <summary>
        /// Логгер для записи информации об ошибках
        /// </summary>
        private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;

        private readonly IWebHostEnvironment _environment = environment;

        #endregion Fields

        #region Public Methods

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

        #endregion Public Methods

        #region Private Methods

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
                    errorResponse.Message = isProduction ? "Ресурс не найден" : exception.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning(exception, "Ресурс не найден: {Message}", exception.Message);
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = "Доступ запрещён";
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning(exception, "Неавторизованный доступ: {Message}", exception.Message);
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = isProduction ? "Некорректный запрос" : exception.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning(exception, "Некорректный запрос: {Message}", exception.Message);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "Внутренняя ошибка сервера";
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

        #endregion Private Methods
    }

    /// <summary>
    /// Модель ответа об ошибке для клиента
    /// </summary>
    /// <remarks>
    /// Унифицированная модель, которая возвращается клиенту при возникновении любой ошибки.
    /// Содержит HTTP статус-код, сообщение об ошибке и временную метку.
    /// </remarks>
    public class ErrorResponse
    {
        #region Properties

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

        #endregion Properties
    }

    /// <summary>
    /// Extension-методы для регистрации <see cref="GlobalExceptionMiddleware"/> в pipeline приложения
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {
        #region Extension Methods

        /// <summary>
        /// Добавляет глобальный обработчик исключений в pipeline приложения
        /// </summary>
        /// <param name="app">Построитель приложения</param>
        /// <returns>Построитель приложения с добавленным middleware</returns>
        /// <remarks>
        /// Рекомендуется добавлять этот middleware первым в pipeline,
        /// чтобы перехватывать исключения из всех последующих middleware.
        /// </remarks>
        /// <example>
        /// <code>
        /// var app = builder.Build();
        /// app.UseGlobalExceptionHandler(); // Должен быть первым
        /// app.UseRouting();
        /// app.UseAuthentication();
        /// // ...
        /// </code>
        /// </example>
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionMiddleware>();
        }

        #endregion Extension Methods
    }
}

