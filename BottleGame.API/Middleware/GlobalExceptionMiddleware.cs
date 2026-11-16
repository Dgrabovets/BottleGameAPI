using System.Net;
using System.Text.Json;

namespace BottleGame.API.Middleware
{
    /// <summary>
    /// Middleware for global exception handling in the application
    /// </summary>
    /// <remarks>
    /// Catches all unhandled exceptions and returns to the client
    /// a unified JSON response with the error information.
    /// It also logs all errors in one place
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of <see cref="GlobalExceptionMiddleware"/>
    /// </remarks>
    /// <param name="next">Delegate of the next middleware in the pipeline</param>
    /// <param name="logger">Logger used to record errors</param>
    /// <param name="environment">Application environment (in Production the exception details are not sent to the client)</param>
    public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
    {

        /// <summary>
        /// Delegate that calls the next middleware in the pipeline
        /// </summary>
        private readonly RequestDelegate _next = next;

        /// <summary>
        /// Logger used to record error information
        /// </summary>
        private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;

        /// <summary>
        /// Hosting environment, used to decide how much error detail to return
        /// </summary>
        private readonly IWebHostEnvironment _environment = environment;



        /// <summary>
        /// Handles an HTTP request and catches exceptions
        /// </summary>
        /// <param name="context">HTTP context of the current request</param>
        /// <returns>Asynchronous task that handles the request</returns>
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
        /// Handles an exception and builds the response for the client
        /// </summary>
        /// <param name="context">HTTP context of the current request</param>
        /// <param name="exception">The caught exception</param>
        /// <returns>Asynchronous task that sends the response</returns>
        /// <remarks>
        /// Mapping of exceptions to HTTP status codes:
        /// <list type="bullet">
        ///     <item><description><see cref="KeyNotFoundException"/> → 404 Not Found</description></item>
        ///     <item><description><see cref="UnauthorizedAccessException"/> → 401 Unauthorized</description></item>
        ///     <item><description><see cref="ArgumentException"/> → 400 Bad Request</description></item>
        ///     <item><description><see cref="InvalidOperationException"/> → 400 Bad Request</description></item>
        ///     <item><description>All other exceptions → 500 Internal Server Error</description></item>
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
                    _logger.LogWarning(exception, "Resource not found: {Message}", exception.Message);
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = $"Access denied\r\n{exception.Message}";
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning(exception, "Unauthorized access: {Message}", exception.Message);
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = exception.Message;
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogWarning(exception, "Invalid request: {Message}", exception.Message);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = $"Internal server error\r\n{exception.Message}";
                    errorResponse.StatusCode = response.StatusCode;
                    _logger.LogError(exception, "Unhandled exception: {Type}: {Message}. StackTrace: {StackTrace}",
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
