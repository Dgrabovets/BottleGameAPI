using BottleGameServer.Authorization.Models;
using BottleGameServer.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BottleGameServer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; } = null!;
        public DbSet<Ban> Bans { get; set; } = null!;
        public DbSet<BanHistoryEntity> BansHistory { get; set; } = null!;
        public DbSet<GameParams> GameParams { get; set; } = null!;
        public DbSet<GameRound> GameRounds { get; set; } = null!;
        public DbSet<Balance> PlayersBalances { get; set; } = null!;
        public DbSet<Admin> AdminCreds { get; set; } = null!;
        public DbSet<ReferalLink> ReferalLinks { get; set; } = null!;
        public DbSet<InvitedPlayer> InvitedPlayers { get; set; } = null!;
        public DbSet<TransactionType> TransactionTypes { get; set; } = null!;
        public DbSet<TransactionStatus> TransactionStatuses { get; set; } = null!;
        public DbSet<MoneyTransaction> MoneyTransactions { get; set; } = null!;
        public DbSet<PlayerOptions> PlayersOptions { get; set; } = null!;
        public DbSet<AppBalance> AppBalance { get; set; } = null!;
        public DbSet<PlayerPlayedRoundsStatisticForWithdrawal> PlayersPlayedRoundsStatisticForWithdrawals { get; set; } = null!;
        public DbSet<NicePayWebhookLog> NicePayWebhookLogs { get; set; } = null!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка сущности Player
            modelBuilder.Entity<Player>(entity =>
            {
                entity.ToTable("Players");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(p => p.TelegramId).HasColumnName("TelegramId").IsRequired();
                entity.Property(p => p.Name)
                    .HasColumnName("Name")
                    .HasColumnType("varchar")
                    .HasMaxLength(255)
                    .IsRequired();
                entity.Property(p => p.AvatarUrl)
                    .HasColumnName("AvatarUrl")
                    .HasColumnType("varchar")
                    .IsRequired();
                entity.Property(p => p.RegisteredInAppAt).HasColumnName("RegisteredInAppAt").IsRequired();
            });

            // Настройка сущности ReferalLink
            modelBuilder.Entity<ReferalLink>(entity =>
            {
                entity.ToTable("ReferalLinks");
                entity.HasKey(b => b.PlayerId);
                entity.Property(b => b.PlayerId).HasColumnName("PlayerId").IsRequired();
                entity.Property(b => b.Link)
                    .HasColumnName("Link")
                    .HasColumnType("varchar")
                    .IsRequired();
                entity.HasOne(b => b.Player)
                    .WithOne(p => p.ReferalLink)
                    .HasForeignKey<ReferalLink>(b => b.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка сущности InvitedPlayer
            modelBuilder.Entity<InvitedPlayer>(entity =>
            {
                entity.ToTable("InvitedPlayers");
                entity.HasKey(ip => new { ip.PlayerIdInvited, ip.PlayerIdInvitedBy });

                entity.HasOne(ip => ip.PlayerInvited)
                    .WithMany(p => p.InvitedByMe)
                    .HasForeignKey(ip => ip.PlayerIdInvitedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ip => ip.PlayerInvitedBy)

                    .WithOne(p => p.InvitedMe)
                    .HasForeignKey<InvitedPlayer>(ip => ip.PlayerIdInvited)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Настройка сущности Ban
            modelBuilder.Entity<Ban>(entity =>
            {
                entity.ToTable("Bans");
                entity.HasKey(b => b.PlayerId);
                entity.Property(b => b.PlayerId).HasColumnName("PlayerId").IsRequired();

                entity.Property(b => b.BanReason)
                    .HasColumnName("BanReason")
                    .HasColumnType("varchar")
                    .IsRequired();

                entity.Property(b => b.BannedAt).HasColumnName("BannedAt").IsRequired();

                entity.Property(b => b.IsActive).HasColumnName("IsActive").IsRequired();

                entity.Property(b => b.Version)
                    .HasColumnName("Version")
                    .IsRowVersion()
                    .IsConcurrencyToken();

                entity.HasOne(b => b.Player)
                    .WithOne(p => p.Ban)
                    .HasForeignKey<Ban>(b => b.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка сущности BanHistoryEntity
            modelBuilder.Entity<BanHistoryEntity>(entity =>
            {
                entity.ToTable("BansHistory");
                entity.HasKey(b => b.Id);
                entity.Property(b => b.PlayerId).HasColumnName("PlayerId").IsRequired();

                entity.Property(b => b.BanReason)
                    .HasColumnName("BanReason")
                    .HasColumnType("varchar")
                    .IsRequired();

                entity.Property(b => b.BannedAt).HasColumnName("BannedAt").IsRequired();

                entity.Property(b => b.UnbannedAt).HasColumnName("UnbannedAt");

                entity.Property(b => b.Version)
                    .HasColumnName("Version")
                    .IsRowVersion()
                    .IsConcurrencyToken();

                entity.HasOne(b => b.Player)
                    .WithMany(p => p.BansHistory)
                    .HasForeignKey(b => b.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка сущности PlayerOptions
            modelBuilder.Entity<PlayerOptions>(entity =>
            {
                entity.ToTable("PlayersOptions");
                entity.HasKey(e => e.PlayerId);
                entity.Property(e => e.WinChance).HasColumnName("WinChance").IsRequired();

                entity.Property(e => e.Wallet).HasColumnName("Wallet");

                entity.Property(e => e.Currency)
                    .HasColumnName("Currency")
                    .HasConversion<string>()
                    .HasColumnType("varchar");

                entity.Property(e => e.PayoutMethod)
                    .HasColumnName("PayoutMethod")
                    .HasConversion<string>()
                    .HasColumnType("varchar")
                    .IsRequired();

                entity.Property(e => e.PaymentMethod)
                    .HasColumnName("PaymentMethod")
                    .HasConversion<string>()
                    .HasColumnType("varchar")
                    .IsRequired();

                entity.HasOne(b => b.Player)
                    .WithOne(p => p.PlayerOptions)
                    .HasForeignKey<PlayerOptions>(b => b.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка сущности GameParams (однострочная таблица)
            modelBuilder.Entity<GameParams>(entity =>
            {
                entity.ToTable("GameParams");
                entity.HasKey(p => p.Id);
            });

            // Настройка сущности Admin
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("AdminCreds");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Id)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();

                entity.Property(a => a.Login)
                    .HasColumnName("Login")
                    .HasColumnType("varchar")  // Явное указание типа
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(a => a.PasswordHash)
                    .HasColumnName("PasswordHash")
                    .HasColumnType("varchar")  // Явное указание типа
                    .IsRequired();

                entity.Property(a => a.Salt)
                    .HasColumnName("Salt")
                    .HasColumnType("varchar");  // Явное указание типа

                entity.Property(a => a.CreatedAt)
                    .HasColumnName("CreatedAt");
            });

            // Настройка сущности GameRound
            modelBuilder.Entity<GameRound>(entity =>
            {
                entity.ToTable("GameRounds");
                entity.HasKey(g => g.Id);
                entity.Property(g => g.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(g => g.PlayerId).HasColumnName("PlayerId").IsRequired();
                entity.Property(g => g.Bet).HasColumnName("Bet").IsRequired();
                entity.Property(g => g.IsWin).HasColumnName("IsWin").IsRequired();
                entity.Property(g => g.FinishedAt).HasColumnName("FinishedAt").IsRequired();

                // Связь Many-to-One с Player
                entity.HasOne(g => g.Player)
                      .WithMany(p => p.GameRounds)
                      .HasForeignKey(g => g.PlayerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка сущности AppBalance
            modelBuilder.Entity<AppBalance>(entity =>
            {
                entity.ToTable("AppBalance");
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(b => b.Amount).HasColumnName("Amount").IsRequired();
                entity.Property(b => b.LowerThreshold).HasColumnName("LowerThreshold").IsRequired();
                entity.Property(b => b.LowerThresholdUpdatedAt).HasColumnName("LowerThresholdUpdatedAt");
                entity.Property(b => b.LowerThresholdAdminIdUpdatedBy).HasColumnName("LowerThresholdAdminIdUpdatedBy");
                entity.Property(b => b.BalanceAmountUpdatedAt).HasColumnName("BalanceAmountUpdatedAt");
                entity.Property(b => b.BalanceAmountAdminIdUpdatedBy).HasColumnName("BalanceAmountAdminIdUpdatedBy");
            });

            // Настройка сущности Balance
            modelBuilder.Entity<Balance>(entity =>
            {
                entity.ToTable("PlayersBalance");
                entity.HasKey(b => b.PlayerId);
                entity.Property(b => b.PlayerId).HasColumnName("PlayerId");
                entity.Property(b => b.Amount).HasColumnName("Amount").IsRequired();

                // Связь 1-to-1 с Player
                entity.HasOne(b => b.Player)
                      .WithOne(p => p.Balance)
                      .HasForeignKey<Balance>(b => b.PlayerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка сущности TransactionType
            modelBuilder.Entity<TransactionType>(entity =>
            {
                entity.ToTable("TransactionTypes");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name)
                    .HasColumnName("Name")
                    .HasColumnType("varchar")
                    .IsRequired();
            });

            // Настройка сущности TransactionStatus
            modelBuilder.Entity<TransactionStatus>(entity =>
            {
                entity.ToTable("TransactionStatuses");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name)
                    .HasColumnName("Name")
                    .HasColumnType("varchar")
                    .IsRequired();
            });

            // Настройка сущности MoneyTransaction
            modelBuilder.Entity<MoneyTransaction>(entity =>
            {
                entity.ToTable("MoneyTransactions");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.PlayerId).HasColumnName("PlayerId");
                entity.Property(t => t.Amount).HasColumnName("Amount").IsRequired();
                entity.Property(t => t.TransactionStatusId).HasColumnName("StatusId").IsRequired();
                entity.Property(t => t.TransactionTypeId).HasColumnName("TransactionTypeId").IsRequired();
                entity.Property(t => t.CreatedAt).HasColumnName("CreatedAt").IsRequired();
                entity.Property(t => t.ProcessedAt).HasColumnName("ProcessedAt");
                entity.Property(t => t.IsProcessingSuccess).HasColumnName("IsProcessingSuccess");

                entity.HasOne(t => t.Player)
                      .WithMany(p => p.MoneyTransactions)
                      .HasForeignKey(b => b.PlayerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.TransactionType)
                      .WithMany(t => t.MoneyTransactions)
                      .HasForeignKey(t => t.TransactionTypeId);

                entity.HasOne(t => t.TransactionStatus)
                      .WithMany(s => s.MoneyTransactions) 
                      .HasForeignKey(t => t.TransactionStatusId);
            });

            // Настройка сущности PlayerPlayedRoundsStatisticForWithdrawal
            modelBuilder.Entity<PlayerPlayedRoundsStatisticForWithdrawal>(entity =>
            {
                entity.ToTable("PlayersPlayedRoundsStatisticsForWithdrawals");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.PlayerId)
                    .HasColumnName("PlayerId")
                    .IsRequired();

                entity.Property(e => e.DepositTransactionId)
                    .HasColumnName("DepositTransactionId")
                    .IsRequired(false);

                entity.Property(e => e.PlayedRoundsCount)
                    .HasColumnName("PlayedRoundsCount")
                    .IsRequired();

                entity.HasOne(e => e.Player)
                    .WithMany()
                    .HasForeignKey(e => e.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.MoneyTransaction)
                    .WithMany()
                    .HasForeignKey(e => e.DepositTransactionId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false);
            });

            // Настройка сущности NicePayWebhookLog для PostgreSQL
            modelBuilder.Entity<NicePayWebhookLog>(entity =>
            {
                entity.ToTable("NicePayWebhookLogs");
                entity.HasKey(l => l.Id);

                entity.Property(l => l.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(l => l.RawRequest).HasColumnName("RawRequest").HasColumnType("varchar").IsRequired();
                entity.Property(l => l.HttpMethod).HasColumnName("HttpMethod").HasColumnType("varchar").HasMaxLength(10).IsRequired();
                entity.Property(l => l.ContentType).HasColumnName("ContentType").HasColumnType("varchar").HasMaxLength(100);
                entity.Property(l => l.IpAddress).HasColumnName("IpAddress").HasColumnType("varchar").HasMaxLength(45);
                entity.Property(l => l.UserAgent).HasColumnName("UserAgent").HasColumnType("varchar").HasMaxLength(500);
                entity.Property(l => l.IsSignatureValid).HasColumnName("IsSignatureValid").IsRequired();
                entity.Property(l => l.ProcessingStatus).HasColumnName("ProcessingStatus").HasColumnType("varchar").HasMaxLength(50).IsRequired();
                entity.Property(l => l.ErrorMessage).HasColumnName("ErrorMessage").HasColumnType("varchar");
                entity.Property(l => l.TransactionId).HasColumnName("TransactionId");
                entity.Property(l => l.WebhookType).HasColumnName("WebhookType").HasColumnType("varchar").HasMaxLength(20);
                entity.Property(l => l.Result).HasColumnName("Result").HasColumnType("varchar").HasMaxLength(50);
                entity.Property(l => l.OrderId).HasColumnName("OrderId").HasColumnType("varchar").HasMaxLength(100);
                entity.Property(l => l.ReceivedAt).HasColumnName("ReceivedAt").IsRequired();
                entity.Property(l => l.ProcessedAt).HasColumnName("ProcessedAt");
            });
        }
    }
}
