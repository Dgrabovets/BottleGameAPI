namespace BottleGame.Application.Common
{
    /// <summary>
    /// Parsing of the player search parameters
    /// </summary>
    public static class PlayerSearchQuery
    {
        /// <summary>
        /// Normalizes telegramId, name and the single q parameter into search criteria
        /// </summary>
        public static (long? TelegramId, string? Name) Resolve(
            long? telegramId,
            string? name,
            string? q)
        {
            if (telegramId.HasValue)
            {
                return (telegramId, string.IsNullOrWhiteSpace(name) ? null : name.Trim());
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return (null, name.Trim());
            }

            if (string.IsNullOrWhiteSpace(q))
            {
                return (null, null);
            }

            var trimmed = q.Trim().TrimStart('@');
            if (trimmed.Length == 0)
            {
                return (null, null);
            }

            if (long.TryParse(trimmed, out var parsedTelegramId))
            {
                return (parsedTelegramId, null);
            }

            return (null, trimmed);
        }
    }
}
