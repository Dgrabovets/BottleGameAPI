using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BottleGame.UnitTests.Validators;

/// <summary>
/// Тесты для FinishRoundRequestValidator
/// </summary>
public class FinishRoundRequestValidatorTests
{
    private readonly FinishRoundRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        // Arrange
        var request = new FinishRoundRequest { Bet = 100, IsWin = true };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_InvalidBet_HasError(decimal bet)
    {
        // Arrange
        var request = new FinishRoundRequest { Bet = bet, IsWin = true };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Bet);
    }

    [Fact]
    public void Validate_ValidBet_NoError()
    {
        // Arrange
        var request = new FinishRoundRequest { Bet = 50, IsWin = false };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Bet);
    }
}

