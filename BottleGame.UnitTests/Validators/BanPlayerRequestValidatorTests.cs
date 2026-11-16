using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Validators;
using FluentValidation.TestHelper;

namespace BottleGame.UnitTests.Validators;

/// <summary>
/// Tests for BanPlayerRequestValidator
/// </summary>
public class BanPlayerRequestValidatorTests
{
    /// <summary>
    /// The validator under test
    /// </summary>
    private readonly BanPlayerRequestValidator _validator = new();

    /// <summary>
    /// A positive player id with a reason passes the validation
    /// </summary>
    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        // Arrange
        var request = new BanPlayerRequest { PlayerId = 1, BanReason = "Нарушение правил" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// A zero or negative player id is rejected
    /// </summary>
    /// <param name="playerId">Player id</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidPlayerId_HasError(int playerId)
    {
        // Arrange
        var request = new BanPlayerRequest { PlayerId = playerId, BanReason = "Reason" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PlayerId);
    }

    /// <summary>
    /// An empty ban reason is rejected
    /// </summary>
    /// <param name="reason">Reason</param>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyBanReason_HasError(string? reason)
    {
        // Arrange
        var request = new BanPlayerRequest { PlayerId = 1, BanReason = reason! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BanReason);
    }
}

