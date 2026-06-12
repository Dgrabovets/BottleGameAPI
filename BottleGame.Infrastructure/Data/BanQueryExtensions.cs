using BottleGame.Domain.Entities;

namespace BottleGame.Infrastructure.Data
{
    /// <summary>
    /// Расширения запросов для фильтрации по активному бану
    /// </summary>
    public static class BanQueryExtensions
    {
        /// <summary>
        /// ID игроков с активным баном
        /// </summary>
        public static IQueryable<int> ActivelyBannedPlayerIds(this ApplicationDbContext context) =>
            context.Bans.Where(b => b.IsActive).Select(b => b.PlayerId);

        /// <summary>
        /// Исключает игроков с активным баном
        /// </summary>
        public static IQueryable<Player> WhereNotActivelyBanned(
            this IQueryable<Player> query,
            IQueryable<int> activelyBannedPlayerIds) =>
            query.Where(p => !activelyBannedPlayerIds.Contains(p.Id));

        /// <summary>
        /// Исключает транзакции игроков с активным баном
        /// </summary>
        public static IQueryable<MoneyTransaction> WherePlayerNotActivelyBanned(
            this IQueryable<MoneyTransaction> query,
            IQueryable<int> activelyBannedPlayerIds) =>
            query.Where(t => !activelyBannedPlayerIds.Contains(t.PlayerId));

        /// <summary>
        /// Исключает балансы игроков с активным баном
        /// </summary>
        public static IQueryable<Balance> WherePlayerNotActivelyBanned(
            this IQueryable<Balance> query,
            IQueryable<int> activelyBannedPlayerIds) =>
            query.Where(b => !activelyBannedPlayerIds.Contains(b.PlayerId));
    }
}
