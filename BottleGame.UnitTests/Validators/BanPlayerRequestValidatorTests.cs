using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BottleGame.UnitTests.Validators;

/// <summary>
/// Тесты для BanPlayerRequestValidator
/// </summary>
public class BanPlayerRequestValidatorTests
{
    private readonly BanPlayerRequestValidator _validator = new();

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

