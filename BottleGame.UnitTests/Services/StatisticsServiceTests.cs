using BottleGame.Application.DTOs.Requests;
using BottleGame.Domain.Constants;
using BottleGame.Infrastructure.Services;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Тесты статистики администратора (общая, за период, timeline)
/// </summary>
public class StatisticsServiceTests : IDisposable
{
    /// <summary>
    /// Jan15.
    /// </summary>
    private static readonly DateTime Jan15 = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// May10.
    /// </summary>
    private static readonly DateTime May10 = new(2026, 5, 10, 14, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// May20.
    /// </summary>
    private static readonly DateTime May20 = new(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// May25.
    /// </summary>
    private static readonly DateTime May25 = new(2026, 5, 25, 16, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Jun10.
    /// </summary>
    private static readonly DateTime Jun10 = new(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Database context of the test.
    /// </summary>
    private readonly Infrastructure.Data.ApplicationDbContext _context;

    /// <summary>
    /// The service under test.
    /// </summary>
    private readonly StatisticsService _service;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test.
    /// </summary>
    public StatisticsServiceTests()
    {
        (_context, var builder) = StatisticsTestDataBuilder.Create();

        var player1 = builder.AddPlayer(Jan15, balance: 2_000m, name: "PlayerJan");
        var player2 = builder.AddPlayer(May20, balance: 3_500m, name: "PlayerMay");
        var player3 = builder.AddPlayer(Jun10, balance: 1_000m, name: "PlayerJun");

        // Успешные операции (должны войти в доход / пополнения / выводы)
        builder.AddSuccessfulDeposit(player1.Id, 5_000m, May10);
        builder.AddSuccessfulWithdrawal(player1.Id, 1_500m, May25);

        // Не должны влиять на доход и пополнения
        builder.AddPendingDeposit(player2.Id, 1_000m);
        builder.AddFailedDeposit(player2.Id, 2_000m, May10);
        builder.AddTransaction(
            player2.Id,
            TransactionTypes.Deposit,
            TransactionStatuses.Approved,
            3_000m,
            isProcessingSuccess: false,
            processedAt: May10);

        // Выводы в обработке
        builder.AddPendingWithdrawal(player2.Id, 800m);
        builder.AddApprovedWithdrawalAwaitingProcessing(player3.Id, 400m);

        _service = new StatisticsService(
            _context,
            new Mock<ILogger<StatisticsService>>().Object);
    }

    /// <summary>
    /// Releases the database context used by the tests.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetStatistics — общая статистика

    /// <summary>
    /// Get statistics: without period; returns all time metrics.
    /// </summary>
    [Fact]
    public async Task GetStatistics_WithoutPeriod_ReturnsAllTimeMetrics()
    {
        // Act
        var result = await _service.GetStatistics();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.UsersTotal.Should().Be(3);
        result.Value.BalancesTotal.Should().Be(6_500m);
        result.Value.DepositsTotal.Should().Be(5_000m);
        result.Value.WithdrawalsTotal.Should().Be(1_500m);
        result.Value.IncomeTotal.Should().Be(3_500m);
        result.Value.PendingWithdrawalsTotal.Should().Be(800m);
    }

    /// <summary>
    /// Get statistics: excludes actively banned player.
    /// </summary>
    [Fact]
    public async Task GetStatistics_ExcludesActivelyBannedPlayer()
    {
        var (context, builder) = StatisticsTestDataBuilder.Create();
        var active = builder.AddPlayer(May20, 1_000m, "Active");
        var banned = builder.AddPlayer(May20, 2_000m, "Banned");
        TestDbContextFactory.AddActiveBan(context, banned.Id);

        builder.AddSuccessfulDeposit(active.Id, 300m, May10);
        builder.AddSuccessfulDeposit(banned.Id, 900m, May10);
        builder.AddPendingWithdrawal(banned.Id, 200m);

        var service = new StatisticsService(
            context,
            new Mock<ILogger<StatisticsService>>().Object);

        var result = await service.GetStatistics();

        result.IsSuccess.Should().BeTrue();
        result.Value!.UsersTotal.Should().Be(1);
        result.Value.BalancesTotal.Should().Be(1_000m);
        result.Value.DepositsTotal.Should().Be(300m);
        result.Value.PendingWithdrawalsTotal.Should().Be(0m);

        context.Dispose();
    }

    /// <summary>
    /// Get statistics: without period; counts only successfully processed transactions.
    /// </summary>
    [Fact]
    public async Task GetStatistics_WithoutPeriod_CountsOnlySuccessfullyProcessedTransactions()
    {
        // Arrange — дополнительный шум в отдельном контексте
        var (context, builder) = StatisticsTestDataBuilder.Create();
        var player = builder.AddPlayer(May20, 100m);
        builder.AddSuccessfulDeposit(player.Id, 100m, May10);
        builder.AddPendingDeposit(player.Id, 999m);
        builder.AddPendingWithdrawal(player.Id, 50m);

        var service = new StatisticsService(
            context,
            new Mock<ILogger<StatisticsService>>().Object);

        // Act
        var result = await service.GetStatistics();

        // Assert
        result.Value!.DepositsTotal.Should().Be(100m);
        result.Value.WithdrawalsTotal.Should().Be(0m);
        result.Value.IncomeTotal.Should().Be(100m);
        result.Value.PendingWithdrawalsTotal.Should().Be(50m);

        context.Dispose();
    }

    /// <summary>
    /// Get statistics: with period; filters users and transactions by dates.
    /// </summary>
    [Fact]
    public async Task GetStatistics_WithPeriod_FiltersUsersAndTransactionsByDates()
    {
        // Act — май 2026: один новый игрок, одно пополнение и один вывод
        var result = await _service.GetStatistics(new GetAppGeneralStatisticsRequest
        {
            DateFrom = new DateTime(2026, 5, 1),
            DateTill = new DateTime(2026, 5, 31)
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.UsersTotal.Should().Be(1, "только игрок, зарегистрированный 20 мая");
        result.Value.DepositsTotal.Should().Be(5_000m);
        result.Value.WithdrawalsTotal.Should().Be(1_500m);
        result.Value.IncomeTotal.Should().Be(3_500m);
        result.Value.BalancesTotal.Should().BeNull("баланс не считается для периода");
        result.Value.PendingWithdrawalsTotal.Should().Be(800m, "pending выводы считаются глобально");
    }

    /// <summary>
    /// Get statistics: with period; excludes transactions outside range.
    /// </summary>
    [Fact]
    public async Task GetStatistics_WithPeriod_ExcludesTransactionsOutsideRange()
    {
        // Arrange
        var (context, builder) = StatisticsTestDataBuilder.Create();
        var player = builder.AddPlayer(Jan15, 0m);
        builder.AddSuccessfulDeposit(player.Id, 1_000m, May10);
        builder.AddSuccessfulDeposit(player.Id, 2_000m, Jun10);

        var service = new StatisticsService(
            context,
            new Mock<ILogger<StatisticsService>>().Object);

        // Act
        var result = await service.GetStatistics(new GetAppGeneralStatisticsRequest
        {
            DateFrom = new DateTime(2026, 6, 1),
            DateTill = new DateTime(2026, 6, 30)
        });

        // Assert
        result.Value!.DepositsTotal.Should().Be(2_000m);
        result.Value.IncomeTotal.Should().Be(2_000m);

        context.Dispose();
    }

    /// <summary>
    /// Get statistics: with only one date; returns failure.
    /// </summary>
    [Fact]
    public async Task GetStatistics_WithOnlyOneDate_ReturnsFailure()
    {
        var result = await _service.GetStatistics(new GetAppGeneralStatisticsRequest
        {
            DateFrom = new DateTime(2026, 5, 1)
        });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("обе даты");
    }

    /// <summary>
    /// Get statistics: with invalid range; returns failure.
    /// </summary>
    [Fact]
    public async Task GetStatistics_WithInvalidRange_ReturnsFailure()
    {
        var result = await _service.GetStatistics(new GetAppGeneralStatisticsRequest
        {
            DateFrom = new DateTime(2026, 6, 1),
            DateTill = new DateTime(2026, 5, 1)
        });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("не может быть позже");
    }

    /// <summary>
    /// Get statistics: with too long range; returns failure.
    /// </summary>
    [Fact]
    public async Task GetStatistics_WithTooLongRange_ReturnsFailure()
    {
        var result = await _service.GetStatistics(new GetAppGeneralStatisticsRequest
        {
            DateFrom = new DateTime(1900, 1, 1),
            DateTill = new DateTime(2100, 1, 1)
        });

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("36500");
    }

    /// <summary>
    /// Get statistics timeline: with multi year period; uses year buckets.
    /// </summary>
    [Fact]
    public async Task GetStatisticsTimeline_WithMultiYearPeriod_UsesYearBuckets()
    {
        var result = await _service.GetStatisticsTimeline(new GetAppStatisticsTimelineRequest
        {
            DateFrom = new DateTime(2004, 1, 1),
            DateTill = new DateTime(2026, 5, 29)
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Bucket.Should().Be("year");
        result.Value.Points.Count.Should().BeInRange(20, 25);
    }

    #endregion GetStatistics — общая статистика

    #region GetStatisticsTimeline

    /// <summary>
    /// Get statistics timeline: returns daily buckets for short period.
    /// </summary>
    [Fact]
    public async Task GetStatisticsTimeline_ReturnsDailyBucketsForShortPeriod()
    {
        // Act
        var result = await _service.GetStatisticsTimeline(new GetAppStatisticsTimelineRequest
        {
            DateFrom = new DateTime(2026, 5, 1),
            DateTill = new DateTime(2026, 5, 31)
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Bucket.Should().Be("day");
        result.Value.Points.Should().NotBeEmpty();

        var may10 = result.Value.Points.Single(p => p.Date.Date == May10.Date);
        may10.DepositsAmount.Should().Be(5_000m);
        may10.WithdrawalsAmount.Should().Be(0m);
        may10.IncomeAmount.Should().Be(5_000m);
        may10.UsersCount.Should().Be(1, "к концу 10 мая зарегистрирован только первый игрок");

        var may25 = result.Value.Points.Single(p => p.Date.Date == May25.Date);
        may25.WithdrawalsAmount.Should().Be(1_500m);
        may25.IncomeAmount.Should().Be(-1_500m);

        var may20Point = result.Value.Points.Single(p => p.Date.Date == May20.Date);
        may20Point.UsersCount.Should().Be(2);
    }

    /// <summary>
    /// Get statistics timeline: ignores unprocessed transactions.
    /// </summary>
    [Fact]
    public async Task GetStatisticsTimeline_IgnoresUnprocessedTransactions()
    {
        // Arrange
        var (context, builder) = StatisticsTestDataBuilder.Create();
        var player = builder.AddPlayer(May20, 0m);
        builder.AddSuccessfulDeposit(player.Id, 500m, May10);
        builder.AddPendingDeposit(player.Id, 9_999m);
        builder.AddPendingWithdrawal(player.Id, 300m);

        var service = new StatisticsService(
            context,
            new Mock<ILogger<StatisticsService>>().Object);

        // Act
        var result = await service.GetStatisticsTimeline(new GetAppStatisticsTimelineRequest
        {
            DateFrom = new DateTime(2026, 5, 1),
            DateTill = new DateTime(2026, 5, 31)
        });

        // Assert
        var may10 = result.Value!.Points.Single(p => p.Date.Date == May10.Date);
        may10.DepositsAmount.Should().Be(500m);
        may10.WithdrawalsAmount.Should().Be(0m);

        context.Dispose();
    }

    /// <summary>
    /// Get statistics timeline: with long period; uses week or month buckets.
    /// </summary>
    [Fact]
    public async Task GetStatisticsTimeline_WithLongPeriod_UsesWeekOrMonthBuckets()
    {
        // Act — больше 90 дней
        var result = await _service.GetStatisticsTimeline(new GetAppStatisticsTimelineRequest
        {
            DateFrom = new DateTime(2026, 1, 1),
            DateTill = new DateTime(2026, 6, 30)
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Bucket.Should().BeOneOf("week", "month");
        result.Value.Points.Count.Should().BeLessThan(90);
    }

    #endregion GetStatisticsTimeline
}
