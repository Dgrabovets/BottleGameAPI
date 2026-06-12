using BottleGame.Domain.Entities;

namespace BottleGame.Infrastructure.Data
{
    /// <summary>
    /// Расширения запросов для фильтрации по активному бану
    /// </summary>
    public static class BanQueryExtensions
    {
        /// <summary>
        /// Исключает игроков с активным баном
        /// </summary>
        public static IQueryable<Player> WhereNotActivelyBanned(
            this IQueryable<Player> query,
            ApplicationDbContext context) =>
            query.Where(p => !context.Bans.Any(b => b.PlayerId == p.Id && b.IsActive));

        /// <summary>
        /// Исключает транзакции игроков с активным баном
        /// </summary>
        public static IQueryable<MoneyTransaction> WherePlayerNotActivelyBanned(
            this IQueryable<MoneyTransaction> query,
            ApplicationDbContext context) =>
            query.Where(t => !context.Bans.Any(b => b.PlayerId == t.PlayerId && b.IsActive));

        /// <summary>
        /// Исключает балансы игроков с активным баном
        /// </summary>
        public static IQueryable<Balance> WherePlayerNotActivelyBanned(
            this IQueryable<Balance> query,
            ApplicationDbContext context) =>
            query.Where(b => !context.Bans.Any(ban => ban.PlayerId == b.PlayerId && ban.IsActive));
    }
}
