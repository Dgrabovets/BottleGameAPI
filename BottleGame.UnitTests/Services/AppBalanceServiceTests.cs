using BottleGame.Application.DTOs.Requests;
using BottleGame.Infrastructure.Services;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Tests for AppBalanceService
/// </summary>
public class AppBalanceServiceTests : IDisposable
{
    /// <summary>
    /// Database context of the test
    /// </summary>
    private readonly Infrastructure.Data.ApplicationDbContext _context;

    /// <summary>
    /// The application balance service under test
    /// </summary>
    private readonly AppBalanceService _balanceService;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test
    /// </summary>
    public AppBalanceServiceTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();
        _balanceService = new AppBalanceService(_context, new Mock<ILogger<AppBalanceService>>().Object);
    }

    /// <summary>
    /// Releases the database context used by the tests
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAppBalance Tests

    /// <summary>
    /// Get app balance: exists; returns balance
    /// </summary>
    [Fact]
    public async Task GetAppBalance_Exists_ReturnsBalance()
    {
        // Act
        var result = await _balanceService.GetAppBalance();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100000);
    }

    #endregion GetAppBalance Tests

    #region UpdateAppBalanceThreshold Tests

    /// <summary>
    /// Update app balance threshold: success
    /// </summary>
    [Fact]
    public async Task UpdateAppBalanceThreshold_Success()
    {
        // Arrange
        var request = new UpdateAppBalanceThresholdRequest { LowerThreshold = 5000 };

        // Act
        var result = await _balanceService.UpdateAppBalanceThreshold(request, 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var balance = _context.AppBalance.First();
        balance.LowerThreshold.Should().Be(5000);
    }

    #endregion UpdateAppBalanceThreshold Tests

    #region TopUpAppBalance Tests

    /// <summary>
    /// Top up app balance: success
    /// </summary>
    [Fact]
    public async Task TopUpAppBalance_Success()
    {
        // Arrange
        var initialAmount = _context.AppBalance.First().Amount;
        var request = new TopUpAppBalanceRequest { Amount = 10000 };

        // Act
        var result = await _balanceService.TopUpAppBalance(request, 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var balance = _context.AppBalance.First();
        balance.Amount.Should().Be(initialAmount + 10000);
    }

    #endregion TopUpAppBalance Tests
}
