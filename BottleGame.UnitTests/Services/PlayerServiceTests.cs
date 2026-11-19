using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Services;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Tests for PlayerService
/// </summary>
public class PlayerServiceTests : IDisposable
{
    /// <summary>
    /// Database context of the test
    /// </summary>
    private readonly Infrastructure.Data.ApplicationDbContext _context;

    /// <summary>
    /// The service under test
    /// </summary>
    private readonly PlayerService _service;

    /// <summary>
    /// Mock of the logger
    /// </summary>
    private readonly Mock<ILogger<PlayerService>> _loggerMock;

    /// <summary>
    /// Mock of the currency service
    /// </summary>
    private readonly Mock<ICurrencyService> _currencyServiceMock;

    /// <summary>
    /// The statistics service under test
    /// </summary>
    private readonly PlayerStatisticsService _statisticsService;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test
    /// </summary>
    public PlayerServiceTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();
        _loggerMock = new Mock<ILogger<PlayerService>>();
        _currencyServiceMock = new Mock<ICurrencyService>();

        _statisticsService = new PlayerStatisticsService(_context, new Mock<ILogger<PlayerStatisticsService>>().Object, new Mock<ICacheService>().Object);
        _service = new PlayerService(_context, _loggerMock.Object, _currencyServiceMock.Object);
    }

    /// <summary>
    /// Releases the database context used by the tests
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region IsBanned Tests

    /// <summary>
    /// Is banned: player not found; returns not found
    /// </summary>
    [Fact]
    public async Task IsBanned_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.IsBanned(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Is banned: player not banned; returns false
    /// </summary>
    [Fact]
    public async Task IsBanned_PlayerNotBanned_ReturnsFalse()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);

        // Act
        var result = await _service.IsBanned(player.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsBanned.Should().BeFalse();
    }

    /// <summary>
    /// Is banned: player banned; returns true
    /// </summary>
    [Fact]
    public async Task IsBanned_PlayerBanned_ReturnsTrue()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var ban = new Ban
        {
            PlayerId = player.Id,
            IsActive = true,
            BanReason = "Test ban",
            BannedAt = DateTime.UtcNow
        };

        _context.Bans.Add(ban);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsBanned(player.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsBanned.Should().BeTrue();
        result.Value.BanReason.Should().Be("Test ban");
    }

    #endregion IsBanned Tests

    #region GetPlayerById Tests

    /// <summary>
    /// Get player by id: player not found; returns not found
    /// </summary>
    [Fact]
    public async Task GetPlayerById_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetPlayerById(999, null, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Get player by id: as admin; returns player
    /// </summary>
    [Fact]
    public async Task GetPlayerById_AsAdmin_ReturnsPlayer()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);

        // Act
        var result = await _service.GetPlayerById(player.Id, null, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Player.Id.Should().Be(player.Id);
        result.Value.Player.Name.Should().Be("TestPlayer");
    }

    /// <summary>
    /// Get player by id: as player requesting own data; returns player
    /// </summary>
    [Fact]
    public async Task GetPlayerById_AsPlayerRequestingOwnData_ReturnsPlayer()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);

        // Act
        var result = await _service.GetPlayerById(player.Id, player.Id, false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Player.Id.Should().Be(player.Id);
    }

    /// <summary>
    /// Get player by id: as player requesting other data; returns unauthorized
    /// </summary>
    [Fact]
    public async Task GetPlayerById_AsPlayerRequestingOtherData_ReturnsUnauthorized()
    {
        // Arrange
        var player1 = TestDbContextFactory.CreateTestPlayer(_context, 111);
        var player2 = TestDbContextFactory.CreateTestPlayer(_context, 222);

        // Act
        var result = await _service.GetPlayerById(player1.Id, player2.Id, false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    #endregion GetPlayerById Tests

    #region GetAllPlayers Tests

    /// <summary>
    /// Get all players: no players; returns not found
    /// </summary>
    [Fact]
    public async Task GetAllPlayers_NoPlayers_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetAllPlayers();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Get all players: with players; returns all
    /// </summary>
    [Fact]
    public async Task GetAllPlayers_WithPlayers_ReturnsAll()
    {
        // Arrange
        TestDbContextFactory.CreateTestPlayer(_context, 111);
        TestDbContextFactory.CreateTestPlayer(_context, 222);

        // Act
        var result = await _service.GetAllPlayers();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Count().Should().Be(2);
    }

    /// <summary>
    /// Get all players: excludes actively banned
    /// </summary>
    [Fact]
    public async Task GetAllPlayers_ExcludesActivelyBanned()
    {
        var active = TestDbContextFactory.CreateTestPlayer(_context, 111);
        var banned = TestDbContextFactory.CreateTestPlayer(_context, 222);
        TestDbContextFactory.AddActiveBan(_context, banned.Id);

        var result = await _service.GetAllPlayers();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value!.Single().Player.Id.Should().Be(active.Id);
    }

    /// <summary>
    /// Get all players: search by telegram id; includes banned
    /// </summary>
    [Fact]
    public async Task GetAllPlayers_SearchByTelegramId_IncludesBanned()
    {
        var banned = TestDbContextFactory.CreateTestPlayer(_context, 625593041);
        banned.Name = "Dgrabovets";
        _context.SaveChanges();
        TestDbContextFactory.AddActiveBan(_context, banned.Id);

        var result = await _service.GetAllPlayers(telegramId: 625593041);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value!.Single().BannedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Get all players: search no results; returns empty list
    /// </summary>
    [Fact]
    public async Task GetAllPlayers_SearchNoResults_ReturnsEmptyList()
    {
        TestDbContextFactory.CreateTestPlayer(_context, 111);

        var result = await _service.GetAllPlayers(name: "nonexistent_user_xyz");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
    }

    /// <summary>
    /// Get all players: search by name; includes banned
    /// </summary>
    [Fact]
    public async Task GetAllPlayers_SearchByName_IncludesBanned()
    {
        var banned = TestDbContextFactory.CreateTestPlayer(_context, 333);
        banned.Name = "UniqueNick";
        _context.SaveChanges();
        TestDbContextFactory.AddActiveBan(_context, banned.Id);

        var result = await _service.GetAllPlayers(name: "uniquenick");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value!.Single().Player.Name.Should().Be("UniqueNick");
    }

    /// <summary>
    /// A page holds only its part of the list and the count is the size of the whole list
    /// </summary>
    [Fact]
    public async Task GetAllPlayers_Paged_ReturnsOnlyThePageAndCountsAll()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            TestDbContextFactory.CreateTestPlayer(_context, 1000 + i);
        }

        // Act
        var second = await _service.GetAllPlayers(page: new PageRequest(2, 2));
        var last = await _service.GetAllPlayers(page: new PageRequest(3, 2));
        var total = await _service.CountPlayers();

        // Assert
        second.Value!.Select(p => p.Player.TelegramId).Should().Equal(1003, 1004);
        last.Value!.Select(p => p.Player.TelegramId).Should().Equal(1005);
        total.Should().Be(5);
    }

    /// <summary>
    /// A page after the last one is an empty list, not an error
    /// </summary>
    [Fact]
    public async Task GetAllPlayers_PageAfterTheLast_ReturnsEmptyList()
    {
        TestDbContextFactory.CreateTestPlayer(_context, 111);

        var result = await _service.GetAllPlayers(page: new PageRequest(5, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    /// <summary>
    /// The count leaves out actively banned players, as the list does
    /// </summary>
    [Fact]
    public async Task CountPlayers_ExcludesActivelyBanned()
    {
        TestDbContextFactory.CreateTestPlayer(_context, 111);
        var banned = TestDbContextFactory.CreateTestPlayer(_context, 222);
        TestDbContextFactory.AddActiveBan(_context, banned.Id);

        (await _service.CountPlayers()).Should().Be(1);
        (await _service.CountPlayers(telegramId: 222)).Should().Be(1);
    }

    #endregion GetAllPlayers Tests

    #region EditPlayer Tests

    /// <summary>
    /// Edit player: player not found; returns not found
    /// </summary>
    [Fact]
    public async Task EditPlayer_PlayerNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new EditPlayerRequest { Name = "NewName" };

        // Act
        var result = await _service.EditPlayer(999, request, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Edit player: update name; success
    /// </summary>
    [Fact]
    public async Task EditPlayer_UpdateName_Success()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var request = new EditPlayerRequest { Name = "UpdatedName" };

        // Act
        var result = await _service.EditPlayer(player.Id, request, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        updatedPlayer!.Name.Should().Be("UpdatedName");
    }

    /// <summary>
    /// Edit player: update balance; as admin, success
    /// </summary>
    [Fact]
    public async Task EditPlayer_UpdateBalance_AsAdmin_Success()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var request = new EditPlayerRequest { Balance = 5000 };

        // Act
        var result = await _service.EditPlayer(player.Id, request, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion EditPlayer Tests

    #region GetPlayerStatistics Tests

    /// <summary>
    /// Get player statistics: player not found; returns not found
    /// </summary>
    [Fact]
    public async Task GetPlayerStatistics_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _statisticsService.GetPlayerStatistics(999, null, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Get player statistics: valid player; returns statistics
    /// </summary>
    [Fact]
    public async Task GetPlayerStatistics_ValidPlayer_ReturnsStatistics()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);

        // Act
        var result = await _statisticsService.GetPlayerStatistics(player.Id, null, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Player.Id.Should().Be(player.Id);
        result.Value.RoundsQty.Should().Be(0);
    }

    #endregion GetPlayerStatistics Tests
}
