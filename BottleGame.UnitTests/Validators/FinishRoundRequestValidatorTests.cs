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
    /// A positive bet within the limit passes the validation
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
    /// A zero, negative or too large bet is rejected
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
    /// Bets inside the allowed range pass the validation
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
