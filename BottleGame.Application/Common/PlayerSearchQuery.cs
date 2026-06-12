namespace BottleGame.Application.Common
{
    /// <summary>
    /// Разбор параметров поиска игроков
    /// </summary>
    public static class PlayerSearchQuery
    {
        /// <summary>
        /// Нормализует telegramId, name и единый параметр q в критерии поиска
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
