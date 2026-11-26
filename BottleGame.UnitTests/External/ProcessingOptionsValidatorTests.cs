using BottleGame.Infrastructure.External.Processing;
using FluentAssertions;

namespace BottleGame.UnitTests.External;

/// <summary>
/// Tests for the processing options validator
/// </summary>
public class ProcessingOptionsValidatorTests
{
    /// <summary>
    /// The validator under test
    /// </summary>
    private readonly ProcessingOptionsValidator _validator = new();

    /// <summary>
    /// Creates valid Live-mode options
    /// </summary>
    /// <returns>The created value</returns>
    private static ProcessingOptions ValidLive() => new()
    {
        Mode = ProcessingMode.Live,
        ShopId = "shop",
        SecretKey = "secret",
        ReturnUrl = "https://example.com/return"
    };

    /// <summary>
    /// Simulated mode needs no provider credentials
    /// </summary>
    [Fact]
    public void Simulated_DoesNotRequireCredentials()
    {
        var result = _validator.Validate(null, new ProcessingOptions());

        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Live mode passes when the shop, key and return address are set
    /// </summary>
    [Fact]
    public void Live_WithAllRequiredSettings_Succeeds()
    {
        _validator.Validate(null, ValidLive()).Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Live mode reports every missing setting
    /// </summary>
    [Fact]
    public void Live_WithoutCredentialsAndReturnUrl_ReportsEachMissingSetting()
    {
        var result = _validator.Validate(null, new ProcessingOptions { Mode = ProcessingMode.Live });

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(3);
    }

    /// <summary>
    /// Live mode rejects a relative return address
    /// </summary>
    [Fact]
    public void Live_WithRelativeReturnUrl_Fails()
    {
        var options = ValidLive();
        options.ReturnUrl = "/return";

        _validator.Validate(null, options).Failed.Should().BeTrue();
    }

    /// <summary>
    /// A payout may be repeated for 24 hours by default, the lifetime of an idempotence key at the provider
    /// </summary>
    [Fact]
    public void PayoutRepeatWindow_IsADayByDefault()
    {
        new ProcessingOptions().PayoutRepeatWindowHours.Should().Be(24);
    }

    /// <summary>
    /// Live mode accepts a window from one hour up to the lifetime of the idempotence key
    /// </summary>
    /// <param name="hours">Window in hours</param>
    /// <param name="valid">Whether the settings pass</param>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(24, true)]
    [InlineData(25, false)]
    public void Live_PayoutRepeatWindow_MustStayWithinTheKeyLifetime(int hours, bool valid)
    {
        var options = ValidLive();
        options.PayoutRepeatWindowHours = hours;

        _validator.Validate(null, options).Succeeded.Should().Be(valid);
    }
}
