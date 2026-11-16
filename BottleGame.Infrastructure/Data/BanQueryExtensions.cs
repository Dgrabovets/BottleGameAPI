using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BottleGame.Infrastructure.Data
{
    /// <summary>
    /// Query extensions for filtering by an active ban
    /// </summary>
    public static class BanQueryExtensions
    {
        /// <summary>
        /// Returns the ids of players with an active ban
        /// </summary>
        public static Task<List<int>> GetActivelyBannedPlayerIdsAsync(
            this ApplicationDbContext context) =>
            context.Bans
                .AsNoTracking()
                .Where(b => b.IsActive)
                .Select(b => b.PlayerId)
                .ToListAsync();

        /// <summary>
        /// Excludes players with an active ban
        /// </summary>
        public static IQueryable<Player> WhereNotActivelyBanned(
            this IQueryable<Player> query,
            IReadOnlyCollection<int> bannedPlayerIds)
        {
            if (bannedPlayerIds.Count == 0)
            {
                return query;
            }

            return query.Where(p => !bannedPlayerIds.Contains(p.Id));
        }

        /// <summary>
        /// Excludes transactions of players with an active ban
        /// </summary>
        public static IQueryable<MoneyTransaction> WherePlayerNotActivelyBanned(
            this IQueryable<MoneyTransaction> query,
            IReadOnlyCollection<int> bannedPlayerIds)
        {
            if (bannedPlayerIds.Count == 0)
            {
                return query;
            }

            return query.Where(t => !bannedPlayerIds.Contains(t.PlayerId));
        }

        /// <summary>
        /// Excludes balances of players with an active ban
        /// </summary>
        public static IQueryable<Balance> WherePlayerNotActivelyBanned(
            this IQueryable<Balance> query,
            IReadOnlyCollection<int> bannedPlayerIds)
        {
            if (bannedPlayerIds.Count == 0)
            {
                return query;
            }

            return query.Where(b => !bannedPlayerIds.Contains(b.PlayerId));
        }
    }
}
