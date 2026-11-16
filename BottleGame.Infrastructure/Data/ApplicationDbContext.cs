using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BottleGame.Infrastructure.Data
{
    /// <summary>
    /// Application database context
    /// </summary>
    /// <remarks>
    /// Initializes the database context
    /// </remarks>
    /// <param name="options">Context options</param>
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
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

        #region Configuration

        /// <summary>
        /// Configures the data model
        /// </summary>
        /// <param name="modelBuilder">Model builder</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply all configurations from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // MSK TimeZone Converter: MSK ↔ UTC for all DateTime fields
            var mskConverter = new ValueConverter<DateTime, DateTime>(
                // Save to the database as UTC
                v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified).ToUniversalTime(),
                // Read from the database as MSK
                v => TimeZoneInfo.ConvertTimeFromUtc(v, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"))
            );

            var mskNullableConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified).ToUniversalTime() : null,
                v => v.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(v.Value, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")) : null
            );

            // Apply to all DateTime fields in all entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(mskConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(mskNullableConverter);
                    }
                }
            }
        }


        #endregion Configuration
    }
}

