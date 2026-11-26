using BottleGame.API.Options;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace BottleGame.UnitTests.External;

/// <summary>
/// Tests for TelegramOptionsValidator: a bot token that anyone can know is refused at startup
/// </summary>
public class TelegramOptionsValidatorTests
{
    /// <summary>
    /// Validates the token in the given environment
    /// </summary>
    /// <param name="token">Bot token</param>
    /// <param name="environmentName">Environment name</param>
    /// <returns>true if the token is accepted</returns>
    private static bool IsAccepted(string token, string environmentName)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(x => x.EnvironmentName).Returns(environmentName);

        return new TelegramOptionsValidator(environment.Object)
            .Validate(null, new TelegramOptions { BotToken = token })
            .Succeeded;
    }

    /// <summary>
    /// An empty token or the ENV_ placeholder is refused in every environment
    /// </summary>
    /// <param name="token">Bot token</param>
    /// <param name="environmentName">Environment name</param>
    [Theory]
    [InlineData("", "Development")]
    [InlineData("   ", "Production")]
    [InlineData("ENV_TelegramBotToken", "Development")]
    [InlineData("env_TelegramBotToken", "Production")]
    public void MissingOrPlaceholderToken_IsRefused(string token, string environmentName)
    {
        IsAccepted(token, environmentName).Should().BeFalse();
    }

    /// <summary>
    /// The docker-compose placeholder is accepted only in Development
    /// </summary>
    /// <param name="environmentName">Environment name</param>
    /// <param name="expected">Whether the token is accepted</param>
    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    public void ComposePlaceholder_IsAcceptedOnlyInDevelopment(string environmentName, bool expected)
    {
        IsAccepted("local-development-bot-token", environmentName).Should().Be(expected);
    }

    /// <summary>
    /// A real-looking token is accepted in production
    /// </summary>
    [Fact]
    public void RealToken_IsAccepted()
    {
        IsAccepted("123456789:not-a-real-token-only-for-tests", "Production").Should().BeTrue();
    }
}
