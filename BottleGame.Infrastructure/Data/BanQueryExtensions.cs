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
        public static IQueryable<Player> WhereNotActivelyBanned(this IQueryable<Player> query) =>
            query.Where(p => p.Ban == null || !p.Ban.IsActive);

        /// <summary>
        /// Исключает транзакции игроков с активным баном
        /// </summary>
        public static IQueryable<MoneyTransaction> WherePlayerNotActivelyBanned(
            this IQueryable<MoneyTransaction> query) =>
            query.Where(t => t.Player.Ban == null || !t.Player.Ban.IsActive);

        /// <summary>
        /// Исключает балансы игроков с активным баном
        /// </summary>
        public static IQueryable<Balance> WherePlayerNotActivelyBanned(this IQueryable<Balance> query) =>
            query.Where(b => b.Player.Ban == null || !b.Player.Ban.IsActive);
    }
}
