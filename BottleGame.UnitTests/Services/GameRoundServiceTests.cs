using BottleGame.Application.DTOs.Requests;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Services;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Tests for GameRoundService
/// </summary>
public class GameRoundServiceTests : IDisposable
{
    /// <summary>
    /// Database context of the test
    /// </summary>
    private readonly Infrastructure.Data.ApplicationDbContext _context;
    /// <summary>
    /// The service under test
    /// </summary>
    private readonly GameRoundService _service;
    /// <summary>
    /// Mock of the logger
    /// </summary>
    private readonly Mock<ILogger<GameRoundService>> _loggerMock;
    /// <summary>
    /// Mock of the cache service
    /// </summary>
    private readonly Mock<Application.Interfaces.ICacheService> _cacheServiceMock;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test
    /// </summary>
    public GameRoundServiceTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();
        _loggerMock = new Mock<ILogger<GameRoundService>>();
        _cacheServiceMock = new Mock<Application.Interfaces.ICacheService>();

        var gameParams = _context.GameParams.First();
        _cacheServiceMock.Setup(x => x.GetGameParamsAsync()).ReturnsAsync(gameParams);

        _service = new GameRoundService(_context, _loggerMock.Object, _cacheServiceMock.Object);
    }

    /// <summary>
    /// Releases the database context used by the tests
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region FinishGameRound Tests

    /// <summary>
    /// Finish game round: player not found; returns not found
    /// </summary>
    [Fact]
    public async Task FinishGameRound_PlayerNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new FinishRoundRequest { Bet = 100, IsWin = true };

        // Act
        var result = await _service.FinishGameRound(999, request, null, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Finish game round: as player; other player, unauthorized
    /// </summary>
    [Fact]
    public async Task FinishGameRound_AsPlayer_OtherPlayer_Unauthorized()
    {
        // Arrange
        var player1 = TestDbContextFactory.CreateTestPlayer(_context, 111);
        var player2 = TestDbContextFactory.CreateTestPlayer(_context, 222);
        var request = new FinishRoundRequest { Bet = 100, IsWin = true };

        // Act
        var result = await _service.FinishGameRound(player1.Id, request, player2.Id, false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    /// <summary>
    /// Finish game round: player banned; returns forbidden
    /// </summary>
    [Fact]
    public async Task FinishGameRound_PlayerBanned_ReturnsForbidden()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _context.Bans.Add(new Ban
        {
            PlayerId = player.Id,
            IsActive = true,
            BanReason = "Test",
            BannedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var request = new FinishRoundRequest { Bet = 100, IsWin = true };

        // Act
        var result = await _service.FinishGameRound(player.Id, request, player.Id, false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    /// <summary>
    /// Finish game round: win; increases balance
    /// </summary>
    [Fact]
    public async Task FinishGameRound_Win_IncreasesBalance()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var initialBalance = _context.PlayersBalances.First(b => b.PlayerId == player.Id).Amount;
        var request = new FinishRoundRequest { Bet = 100, IsWin = true };

        // Act
        var result = await _service.FinishGameRound(player.Id, request, player.Id, false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var newBalance = _context.PlayersBalances.First(b => b.PlayerId == player.Id).Amount;
        newBalance.Should().BeGreaterThan(initialBalance);
    }

    /// <summary>
    /// Finish game round: loss; decreases balance
    /// </summary>
    [Fact]
    public async Task FinishGameRound_Loss_DecreasesBalance()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var initialBalance = _context.PlayersBalances.First(b => b.PlayerId == player.Id).Amount;
        var request = new FinishRoundRequest { Bet = 100, IsWin = false };

        // Act
        var result = await _service.FinishGameRound(player.Id, request, player.Id, false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var newBalance = _context.PlayersBalances.First(b => b.PlayerId == player.Id).Amount;
        newBalance.Should().BeLessThan(initialBalance);
    }

    /// <summary>
    /// Finish game round: insufficient balance; returns failure
    /// </summary>
    [Fact]
    public async Task FinishGameRound_InsufficientBalance_ReturnsFailure()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var balance = _context.PlayersBalances.First(b => b.PlayerId == player.Id);
        balance.Amount = 50;
        await _context.SaveChangesAsync();

        var request = new FinishRoundRequest { Bet = 100, IsWin = false };

        // Act
        var result = await _service.FinishGameRound(player.Id, request, player.Id, false);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion FinishGameRound Tests

    #region GetPlayerPlayedRounds Tests

    /// <summary>
    /// Get player played rounds: player not found; returns not found
    /// </summary>
    [Fact]
    public async Task GetPlayerPlayedRounds_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetPlayerPlayedRounds(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Get player played rounds: success; returns rounds
    /// </summary>
    [Fact]
    public async Task GetPlayerPlayedRounds_Success_ReturnsRounds()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _context.GameRounds.Add(new GameRound
        {
            PlayerId = player.Id,
            Bet = 100,
            IsWin = true,
            FinishedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPlayerPlayedRounds(player.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Rounds.Should().HaveCount(1);
    }

    #endregion GetPlayerPlayedRounds Tests

    #region GetProfit Tests

    /// <summary>
    /// Get profit: no rounds; returns not found
    /// </summary>
    [Fact]
    public async Task GetProfit_NoRounds_ReturnsNotFound()
    {
        // Arrange
        var request = new GetProfitRequest();

        // Act
        var result = await _service.GetProfit(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Get profit: with rounds; calculates profit
    /// </summary>
    [Fact]
    public async Task GetProfit_WithRounds_CalculatesProfit()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _context.GameRounds.AddRange(
            new GameRound { PlayerId = player.Id, Bet = 100, IsWin = false, FinishedAt = DateTime.UtcNow },
            new GameRound { PlayerId = player.Id, Bet = 50, IsWin = true, FinishedAt = DateTime.UtcNow }
        );

        await _context.SaveChangesAsync();

        var request = new GetProfitRequest();

        // Act
        var result = await _service.GetProfit(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Profit.Should().Be(100); // Losses only (profit for house)
    }

    #endregion GetProfit Tests
}
