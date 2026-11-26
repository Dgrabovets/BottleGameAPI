using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BottleGame.Infrastructure.Data
{
    /// <summary>
    /// Application database context
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        #region DbSets

        /// <summary>
        /// Players
        /// </summary>
        public DbSet<Player> Players { get; set; } = null!;

        /// <summary>
        /// Player bans
        /// </summary>
        public DbSet<Ban> Bans { get; set; } = null!;

        /// <summary>
        /// Ban history
        /// </summary>
        public DbSet<BanHistoryEntity> BansHistory { get; set; } = null!;

        /// <summary>
        /// Game parameters
        /// </summary>
        public DbSet<GameParams> GameParams { get; set; } = null!;

        /// <summary>
        /// Game rounds
        /// </summary>
        public DbSet<GameRound> GameRounds { get; set; } = null!;

        /// <summary>
        /// Player balances
        /// </summary>
        public DbSet<Balance> PlayersBalances { get; set; } = null!;

        /// <summary>
        /// Administrators
        /// </summary>
        public DbSet<AdministrationRoleUser> AdminCreds { get; set; } = null!;

        /// <summary>
        /// Referral links
        /// </summary>
        public DbSet<ReferralLink> ReferralLinks { get; set; } = null!;

        /// <summary>
        /// Invited players
        /// </summary>
        public DbSet<InvitedPlayer> InvitedPlayers { get; set; } = null!;

        /// <summary>
        /// Transaction types
        /// </summary>
        public DbSet<TransactionType> TransactionTypes { get; set; } = null!;

        /// <summary>
        /// Transaction statuses
        /// </summary>
        public DbSet<TransactionStatus> TransactionStatuses { get; set; } = null!;

        /// <summary>
        /// Money transactions
        /// </summary>
        public DbSet<MoneyTransaction> MoneyTransactions { get; set; } = null!;

        /// <summary>
        /// Player options
        /// </summary>
        public DbSet<PlayerOptions> PlayersOptions { get; set; } = null!;

        /// <summary>
        /// Application balance
        /// </summary>
        public DbSet<AppBalance> AppBalance { get; set; } = null!;

        /// <summary>
        /// Statistics of the rounds played for a withdrawal
        /// </summary>
        public DbSet<PlayerPlayedRoundsStatisticForWithdrawal> PlayersPlayedRoundsStatisticForWithdrawals { get; set; } = null!;

        /// <summary>
        /// Logs of payment processing webhooks
        /// </summary>
        public DbSet<ProcessingWebhookLog> ProcessingWebhookLogs { get; set; } = null!;

        #endregion DbSets

        /// <summary>
        /// Initializes the database context
        /// </summary>
        /// <param name="options">Context options</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        #region Configuration

        /// <summary>
        /// Configures the data model
        /// </summary>
        /// <param name="modelBuilder">Model builder</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply all configurations from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Dates are stored and returned as UTC. The converters only fix the kind: the database returns
            // UTC, so the value is marked as UTC and serialized with a "Z", and a value without a kind is read as UTC.
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => ToUtc(v),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

            var utcNullableConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? ToUtc(v.Value) : null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null
            );

            // Apply to all DateTime fields in all entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(utcNullableConverter);
                    }
                }
            }
        }


        /// <summary>
        /// Converts a date to UTC: a local value is converted, a value without a kind is taken as UTC
        /// </summary>
        /// <param name="value">Date to convert</param>
        /// <returns>The same moment as a UTC date</returns>
        private static DateTime ToUtc(DateTime value) =>
            value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        #endregion Configuration
    }
}
