using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BottleGame.Infrastructure.Data
{
    /// <summary>
    /// Контекст базы данных приложения
    /// </summary>
    /// <remarks>
    /// Инициализирует контекст базы данных
    /// </remarks>
    /// <param name="options">Опции контекста</param>
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        #region DbSets

        /// <summary>
        /// Игроки
        /// </summary>
        public DbSet<Player> Players { get; set; } = null!;

        /// <summary>
        /// Баны игроков
        /// </summary>
        public DbSet<Ban> Bans { get; set; } = null!;

        /// <summary>
        /// История банов
        /// </summary>
        public DbSet<BanHistoryEntity> BansHistory { get; set; } = null!;

        /// <summary>
        /// Игровые параметры
        /// </summary>
        public DbSet<GameParams> GameParams { get; set; } = null!;

        /// <summary>
        /// Игровые раунды
        /// </summary>
        public DbSet<GameRound> GameRounds { get; set; } = null!;

        /// <summary>
        /// Балансы игроков
        /// </summary>
        public DbSet<Balance> PlayersBalances { get; set; } = null!;

        /// <summary>
        /// Администраторы
        /// </summary>
        public DbSet<AdministrationRoleUser> AdminCreds { get; set; } = null!;

        /// <summary>
        /// Реферальные ссылки
        /// </summary>
        public DbSet<ReferralLink> ReferralLinks { get; set; } = null!;

        /// <summary>
        /// Приглашённые игроки
        /// </summary>
        public DbSet<InvitedPlayer> InvitedPlayers { get; set; } = null!;

        /// <summary>
        /// Типы транзакций
        /// </summary>
        public DbSet<TransactionType> TransactionTypes { get; set; } = null!;

        /// <summary>
        /// Статусы транзакций
        /// </summary>
        public DbSet<TransactionStatus> TransactionStatuses { get; set; } = null!;

        /// <summary>
        /// Денежные транзакции
        /// </summary>
        public DbSet<MoneyTransaction> MoneyTransactions { get; set; } = null!;

        /// <summary>
        /// Опции игроков
        /// </summary>
        public DbSet<PlayerOptions> PlayersOptions { get; set; } = null!;

        /// <summary>
        /// Баланс приложения
        /// </summary>
        public DbSet<AppBalance> AppBalance { get; set; } = null!;

        /// <summary>
        /// Статистика сыгранных раундов для вывода
        /// </summary>
        public DbSet<PlayerPlayedRoundsStatisticForWithdrawal> PlayersPlayedRoundsStatisticForWithdrawals { get; set; } = null!;

        /// <summary>
        /// Логи вебхуков платёжного процессинга.
        /// </summary>
        public DbSet<ProcessingWebhookLog> ProcessingWebhookLogs { get; set; } = null!;

        #endregion DbSets

        #region Configuration

        /// <summary>
        /// Настраивает модель данных
        /// </summary>
        /// <param name="modelBuilder">Построитель модели</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Применяем все конфигурации из текущей сборки
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // MSK TimeZone Converter: MSK ↔ UTC для всех DateTime полей
            var mskConverter = new ValueConverter<DateTime, DateTime>(
                // Сохраняем в БД как UTC
                v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified).ToUniversalTime(),
                // Читаем из БД как MSK
                v => TimeZoneInfo.ConvertTimeFromUtc(v, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"))
            );

            var mskNullableConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified).ToUniversalTime() : null,
                v => v.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(v.Value, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")) : null
            );

            // Применить ко всем DateTime полям во всех сущностях
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

