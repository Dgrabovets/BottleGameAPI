using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BottleGame.Infrastructure.Data
{
    /// <summary>
    /// Расширения запросов для фильтрации по активному бану
    /// </summary>
    public static class BanQueryExtensions
    {
        /// <summary>
        /// Возвращает ID игроков с активным баном
        /// </summary>
        public static Task<List<int>> GetActivelyBannedPlayerIdsAsync(
            this ApplicationDbContext context) =>
            context.Bans
                .AsNoTracking()
                .Where(b => b.IsActive)
                .Select(b => b.PlayerId)
                .ToListAsync();

        /// <summary>
        /// Исключает игроков с активным баном
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
        /// Исключает транзакции игроков с активным баном
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
        /// Исключает балансы игроков с активным баном
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
