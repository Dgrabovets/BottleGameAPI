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
/// Тесты для PlayerService
/// </summary>
public class PlayerServiceTests : IDisposable
{
    private readonly Infrastructure.Data.ApplicationDbContext _context;
    private readonly PlayerService _service;
    private readonly Mock<ILogger<PlayerService>> _loggerMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ICurrencyService> _currencyServiceMock;

    public PlayerServiceTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();
        _loggerMock = new Mock<ILogger<PlayerService>>();
        _cacheServiceMock = new Mock<ICacheService>();
        _currencyServiceMock = new Mock<ICurrencyService>();

        _service = new PlayerService(_context, _loggerMock.Object, _cacheServiceMock.Object, _currencyServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region IsBanned Tests

    [Fact]
    public async Task IsBanned_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.IsBanned(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

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

    [Fact]
    public async Task GetPlayerById_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetPlayerById(999, null, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

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

    [Fact]
    public async Task GetAllPlayers_NoPlayers_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetAllPlayers();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

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

    #endregion GetAllPlayers Tests

    #region EditPlayer Tests

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

    [Fact]
    public async Task GetPlayerStatistics_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetPlayerStatistics(999, null, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPlayerStatistics_ValidPlayer_ReturnsStatistics()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);

        // Act
        var result = await _service.GetPlayerStatistics(player.Id, null, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Player.Id.Should().Be(player.Id);
        result.Value.RoundsQty.Should().Be(0);
    }

    #endregion GetPlayerStatistics Tests
}
