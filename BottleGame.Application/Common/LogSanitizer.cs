namespace BottleGame.Application.Common
{
    /// <summary>
    /// Cleans user-controlled text before it is written to the log
    /// </summary>
    public static class LogSanitizer
    {
        /// <summary>
        /// Removes carriage returns and line feeds, so a value cannot start a fake log entry
        /// </summary>
        /// <param name="value">Text that came from a request</param>
        /// <returns>The text without CR and LF, or null when the value is null</returns>
        public static string? Clean(string? value)
        {
            return value?.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }
    }
}
