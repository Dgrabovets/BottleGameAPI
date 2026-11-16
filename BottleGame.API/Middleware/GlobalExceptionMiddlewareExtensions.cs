using System.Net;
using System.Text.Json;

namespace BottleGame.API.Middleware
{
    /// <summary>
    /// Extension-методы для регистрации <see cref="GlobalExceptionMiddleware"/> в pipeline приложения
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {

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

    }
}
