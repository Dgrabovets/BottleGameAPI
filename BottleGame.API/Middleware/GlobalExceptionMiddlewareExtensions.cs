namespace BottleGame.API.Middleware
{
    /// <summary>
    /// Extension methods for registering <see cref="GlobalExceptionMiddleware"/> in the application pipeline
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {

        /// <summary>
        /// Adds the global exception handler to the application pipeline
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <returns>Application builder with the middleware added</returns>
        /// <remarks>
        /// It is recommended to add this middleware first in the pipeline
        /// so that it catches the exceptions from all the following middleware
        /// </remarks>
        /// <example>
        /// <code>
        /// var app = builder.Build();
        /// app.UseGlobalExceptionHandler(); // Must be first
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
