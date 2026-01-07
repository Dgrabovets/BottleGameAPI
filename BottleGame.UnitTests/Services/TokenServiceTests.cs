using System.Security.Claims;
using BottleGame.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Тесты для TokenService
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _tokenService;

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
