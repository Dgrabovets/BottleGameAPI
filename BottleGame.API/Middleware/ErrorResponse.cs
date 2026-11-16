namespace BottleGame.API.Middleware
{
    /// <summary>
    /// Error response model for the client
    /// </summary>
    /// <remarks>
    /// Unified model returned to the client for any error.
    /// Contains the HTTP status code, the error message and a timestamp
    /// </remarks>
    public class ErrorResponse
    {

        /// <summary>
        /// HTTP status code of the error
        /// </summary>
        /// <example>404</example>
        public int StatusCode { get; set; }

        /// <summary>
        /// Human-readable error message
        /// </summary>
        /// <example>Resource not found</example>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the error in UTC
        /// </summary>
        /// <example>2025-12-24T10:30:00Z</example>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
