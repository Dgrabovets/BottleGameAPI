using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BottleGame.UnitTests.Fixtures;

/// <summary>
/// Factory for creating a test database context
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new test context with a unique database name
    /// </summary>
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates a context with pre-filled reference data
    /// </summary>
    public static ApplicationDbContext CreateWithReferenceData()
    {
        var context = Create();
        SeedReferenceData(context);
        return context;
    }

    /// <summary>
    /// Fills the reference data
    /// </summary>
    private static void SeedReferenceData(ApplicationDbContext context)
    {
        // Transaction types
        context.TransactionTypes.AddRange(
            new TransactionType { Id = TransactionTypes.Deposit, Name = "Пополнение" },
            new TransactionType { Id = TransactionTypes.Withdrawal, Name = "Вывод" }
        );

        // Transaction statuses
        context.TransactionStatuses.AddRange(
            new TransactionStatus { Id = TransactionStatuses.Pending, Name = "В ожидании" },
            new TransactionStatus { Id = TransactionStatuses.Approved, Name = "Одобрено" },
            new TransactionStatus { Id = TransactionStatuses.Rejected, Name = "Отклонено" }
        );

        // Game parameters
        context.GameParams.Add(new GameParams
        {
            Id = 1,
            BaseWinChance = 0.5f,
            MinDepositAmount = 100,
            MinRoundsCountForWithdrawal = 5,
            MinWinChance = 0.1f,
            MaxWinChance = 0.9f,
            ChanceDecPerThrowMin = 0.05f,
            ChanceDecPerThrowMax = 0.1f,
            ChanceDecPerRotation = 0.05f,
            MinRotationWinChance = 0.1f,
            MinMultiplier = 1.5f,
            MaxMultiplier = 2.0f,
            InitialBalanceLoss = 0.1f,
            DeviationChance = 0.5f,
            MaxDeviationPercent = 0.5f,
            MinGamesForBoost = 10,
            WinBoostChance = 0.1f,
            WinBoostMultiplier = 2.0f,
            HardResetChance = 0.05f,
            ControlGoal = 100f,
            ReelSpeed = 1.0f,
            UprightThresholdForLine = 0.5f
        });

        // Application balance
        context.AppBalance.Add(new AppBalance
        {
            Id = 1,
            Amount = 100000,
            LowerThreshold = 10000
        });

        context.SaveChanges();
    }

    /// <summary>
    /// Creates a test player
    /// </summary>
    public static Player CreateTestPlayer(ApplicationDbContext context, long telegramId = 123456789)
    {
        var player = new Player
        {
            TelegramId = telegramId,
            Name = "TestPlayer",
            AvatarUrl = "https://example.com/avatar.jpg",
            RegisteredInAppAt = DateTime.UtcNow
        };

        context.Players.Add(player);
        context.SaveChanges();

        // Add the related data
        var balance = new Balance { PlayerId = player.Id, Amount = 1000 };
        var playerOptions = new PlayerOptions
        {
            PlayerId = player.Id,
            WinChance = 0.5f,
            Currency = "rub",
            PaymentMethod = "sbp_rub",
            PayoutMethod = "sbp_rub",
            Wallet = "79001234567"
        };

        var roundsStatistic = new PlayerPlayedRoundsStatisticForWithdrawal
        {
            PlayerId = player.Id,
            PlayedRoundsCount = 0
        };

        context.PlayersBalances.Add(balance);
        context.PlayersOptions.Add(playerOptions);
        context.PlayersPlayedRoundsStatisticForWithdrawals.Add(roundsStatistic);
        context.SaveChanges();

        return player;
    }

    /// <summary>
    /// Assigns an active ban to a player
    /// </summary>
    public static void AddActiveBan(ApplicationDbContext context, int playerId, string reason = "Test ban")
    {
        context.Bans.Add(new Ban
        {
            PlayerId = playerId,
            BanReason = reason,
            BannedAt = DateTime.UtcNow,
            IsActive = true
        });

        context.SaveChanges();
    }
}
