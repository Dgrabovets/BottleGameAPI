using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Validators;
using FluentValidation.TestHelper;

namespace BottleGame.UnitTests.Validators;

/// <summary>
/// Tests for FinishRoundRequestValidator
/// </summary>
public class FinishRoundRequestValidatorTests
{
    /// <summary>
    /// The validator under test
    /// </summary>
    private readonly FinishRoundRequestValidator _validator = new();

    /// <summary>
    /// Validate: valid request; no errors
    /// </summary>
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

    /// <summary>
    /// Validate: invalid bet; has error
    /// </summary>
    /// <param name="bet">Bet</param>
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

    /// <summary>
    /// Validate: valid bet; no error
    /// </summary>
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

