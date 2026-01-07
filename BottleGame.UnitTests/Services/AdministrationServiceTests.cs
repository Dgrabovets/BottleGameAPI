using BottleGame.Application.DTOs.Requests;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Services;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Тесты для AdministrationService
/// </summary>
public class AdministrationServiceTests : IDisposable
{
    private readonly Infrastructure.Data.ApplicationDbContext _context;
    private readonly AdministrationService _service;
    private readonly Mock<ILogger<AdministrationService>> _loggerMock;
    private readonly Mock<Application.Interfaces.IPasswordHasher> _passwordHasherMock;

    public AdministrationServiceTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();
        _loggerMock = new Mock<ILogger<AdministrationService>>();
        _passwordHasherMock = new Mock<Application.Interfaces.IPasswordHasher>();

        _passwordHasherMock.Setup(x => x.HashWithSalt(It.IsAny<string>()))
            .Returns((string pwd) => ($"hashed_{pwd}", "salt"));
        _passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string hash, string pwd, string salt) => hash == $"hashed_{pwd}");

        _service = new AdministrationService(_context, _loggerMock.Object, _passwordHasherMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAppBalance Tests

    [Fact]
    public async Task GetAppBalance_Exists_ReturnsBalance()
    {
        // Act
        var result = await _service.GetAppBalance();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100000);
    }

    #endregion GetAppBalance Tests

    #region UpdateAppBalanceThreshold Tests

    [Fact]
    public async Task UpdateAppBalanceThreshold_Success()
    {
        // Arrange
        var request = new UpdateAppBalanceThresholdRequest { LowerThreshold = 5000 };

        // Act
        var result = await _service.UpdateAppBalanceThreshold(request, 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var balance = _context.AppBalance.First();
        balance.LowerThreshold.Should().Be(5000);
    }

    #endregion UpdateAppBalanceThreshold Tests

    #region TopUpAppBalance Tests

    [Fact]
    public async Task TopUpAppBalance_Success()
    {
        // Arrange
        var initialAmount = _context.AppBalance.First().Amount;
        var request = new TopUpAppBalanceRequest { Amount = 10000 };

        // Act
        var result = await _service.TopUpAppBalance(request, 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var balance = _context.AppBalance.First();
        balance.Amount.Should().Be(initialAmount + 10000);
    }

    #endregion TopUpAppBalance Tests

    #region RegisterModerator Tests

    [Fact]
    public async Task RegisterModerator_Success()
    {
        // Arrange
        var request = new AdministrationRoleUserCredentials { Login = "moderator", Password = "password123" };

        // Act
        var result = await _service.RegisterModerator(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var moderator = _context.AdminCreds.FirstOrDefault(a => a.Login == "moderator");
        moderator.Should().NotBeNull();
        moderator!.Role.Should().Be(AdministrationRoles.Moderator);
    }

    [Fact]
    public async Task RegisterModerator_AlreadyExists_ReturnsConflict()
    {
        // Arrange
        _context.AdminCreds.Add(new AdministrationRoleUser
        {
            Login = "existing",
            PasswordHash = "hash",
            Salt = "salt",
            Role = AdministrationRoles.Moderator,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new AdministrationRoleUserCredentials { Login = "existing", Password = "password123" };

        // Act
        var result = await _service.RegisterModerator(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    #endregion RegisterModerator Tests

    #region GetModerators Tests

    [Fact]
    public async Task GetModerators_ReturnsAllModerators()
    {
        // Arrange
        _context.AdminCreds.AddRange(
            new AdministrationRoleUser { Login = "mod1", PasswordHash = "h1", Salt = "s1", Role = AdministrationRoles.Moderator, CreatedAt = DateTime.UtcNow },
            new AdministrationRoleUser { Login = "mod2", PasswordHash = "h2", Salt = "s2", Role = AdministrationRoles.Moderator, CreatedAt = DateTime.UtcNow },
            new AdministrationRoleUser { Login = "admin", PasswordHash = "h3", Salt = "s3", Role = AdministrationRoles.Admin, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetModerators(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var moderators = result.Value as IEnumerable<Application.DTOs.Responses.ModeratorDTO>;
        moderators.Should().HaveCount(2);
    }

    #endregion GetModerators Tests

    #region DeleteModerator Tests

    [Fact]
    public async Task DeleteModerator_NotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.DeleteModerator(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteModerator_Success()
    {
        // Arrange
        var moderator = new AdministrationRoleUser
        {
            Login = "toDelete",
            PasswordHash = "h",
            Salt = "s",
            Role = AdministrationRoles.Moderator,
            CreatedAt = DateTime.UtcNow
        };
        _context.AdminCreds.Add(moderator);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteModerator(moderator.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.AdminCreds.Any(a => a.Id == moderator.Id).Should().BeFalse();
    }

    #endregion DeleteModerator Tests
}
