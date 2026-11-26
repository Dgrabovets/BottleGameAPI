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
/// Tests for ModeratorService
/// </summary>
public class ModeratorServiceTests : IDisposable
{
    /// <summary>
    /// Database context of the test
    /// </summary>
    private readonly Infrastructure.Data.ApplicationDbContext _context;

    /// <summary>
    /// The moderator service under test
    /// </summary>
    private readonly ModeratorService _moderatorService;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test
    /// </summary>
    public ModeratorServiceTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();
        var passwordHasherMock = new Mock<Application.Interfaces.IPasswordHasher>();

        passwordHasherMock.Setup(x => x.HashWithSalt(It.IsAny<string>()))
            .Returns((string pwd) => ($"hashed_{pwd}", "salt"));

        passwordHasherMock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string hash, string pwd, string salt) => hash == $"hashed_{pwd}");

        _moderatorService = new ModeratorService(_context, new Mock<ILogger<ModeratorService>>().Object, passwordHasherMock.Object);
    }

    /// <summary>
    /// Releases the database context used by the tests
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region RegisterModerator Tests

    /// <summary>
    /// Register moderator: success
    /// </summary>
    [Fact]
    public async Task RegisterModerator_Success()
    {
        // Arrange
        var request = new AdministrationRoleUserCredentials { Login = "moderator", Password = "password123" };

        // Act
        var result = await _moderatorService.RegisterModerator(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var moderator = _context.AdminCreds.FirstOrDefault(a => a.Login == "moderator");
        moderator.Should().NotBeNull();
        moderator!.Role.Should().Be(AdministrationRoles.MODERATOR);
    }

    /// <summary>
    /// Register moderator: already exists; returns conflict
    /// </summary>
    [Fact]
    public async Task RegisterModerator_AlreadyExists_ReturnsConflict()
    {
        // Arrange
        _context.AdminCreds.Add(new AdministrationRoleUser
        {
            Login = "existing",
            PasswordHash = "hash",
            Salt = "salt",
            Role = AdministrationRoles.MODERATOR,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var request = new AdministrationRoleUserCredentials { Login = "existing", Password = "password123" };

        // Act
        var result = await _moderatorService.RegisterModerator(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    #endregion RegisterModerator Tests

    #region GetModerators Tests

    /// <summary>
    /// Get moderators: returns all moderators
    /// </summary>
    [Fact]
    public async Task GetModerators_ReturnsAllModerators()
    {
        // Arrange
        _context.AdminCreds.AddRange(
            new AdministrationRoleUser { Login = "mod1", PasswordHash = "h1", Salt = "s1", Role = AdministrationRoles.MODERATOR, CreatedAt = DateTime.UtcNow },
            new AdministrationRoleUser { Login = "mod2", PasswordHash = "h2", Salt = "s2", Role = AdministrationRoles.MODERATOR, CreatedAt = DateTime.UtcNow },
            new AdministrationRoleUser { Login = "admin", PasswordHash = "h3", Salt = "s3", Role = AdministrationRoles.ADMIN, CreatedAt = DateTime.UtcNow }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _moderatorService.GetModerators(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var moderators = result.Value as IEnumerable<Application.DTOs.Responses.ModeratorDTO>;
        moderators.Should().HaveCount(2);
    }

    #endregion GetModerators Tests

    #region DeleteModerator Tests

    /// <summary>
    /// Delete moderator: not found; returns not found
    /// </summary>
    [Fact]
    public async Task DeleteModerator_NotFound_ReturnsNotFound()
    {
        // Act
        var result = await _moderatorService.DeleteModerator(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Delete moderator: success
    /// </summary>
    [Fact]
    public async Task DeleteModerator_Success()
    {
        // Arrange
        var moderator = new AdministrationRoleUser
        {
            Login = "toDelete",
            PasswordHash = "h",
            Salt = "s",
            Role = AdministrationRoles.MODERATOR,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdminCreds.Add(moderator);
        await _context.SaveChangesAsync();

        // Act
        var result = await _moderatorService.DeleteModerator(moderator.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.AdminCreds.Any(a => a.Id == moderator.Id).Should().BeFalse();
    }

    #endregion DeleteModerator Tests
}
