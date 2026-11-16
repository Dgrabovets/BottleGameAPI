using BottleGame.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Tests for TokenService
/// </summary>
public class TokenServiceTests
{
    /// <summary>
    /// Token service
    /// </summary>
    private readonly TokenService _tokenService;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test
    /// </summary>
    public TokenServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "JWT:Key", "this_is_a_very_long_secret_key_for_testing_purposes_12345" },
            { "JWT:Issuer", "TestIssuer" },
            { "JWT:Audience", "TestAudience" },
            { "JWT:ExpireMinutes", "60" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _tokenService = new TokenService(config);
    }

    /// <summary>
    /// Generate token: with claims; returns non empty token
    /// </summary>
    [Fact]
    public void GenerateToken_WithClaims_ReturnsNonEmptyToken()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Name, "TestUser"),
            new(ClaimTypes.Role, "Player")
        };

        // Act
        var token = _tokenService.GenerateToken(claims);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Generate token: returns jwt format
    /// </summary>
    [Fact]
    public void GenerateToken_ReturnsJwtFormat()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1")
        };

        // Act
        var token = _tokenService.GenerateToken(claims);

        // Assert
        token.Should().Contain(".");
        token.Split('.').Should().HaveCount(3); // JWT format: header.payload.signature
    }

    /// <summary>
    /// Generate token: different claims; returns different tokens
    /// </summary>
    [Fact]
    public void GenerateToken_DifferentClaims_ReturnsDifferentTokens()
    {
        // Arrange
        var claims1 = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        var claims2 = new List<Claim> { new(ClaimTypes.NameIdentifier, "2") };

        // Act
        var token1 = _tokenService.GenerateToken(claims1);
        var token2 = _tokenService.GenerateToken(claims2);

        // Assert
        token1.Should().NotBe(token2);
    }
}
