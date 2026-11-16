using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Validators;
using FluentValidation.TestHelper;

namespace BottleGame.UnitTests.Validators;

/// <summary>
/// Tests for MoneyTransactionRequestValidator
/// </summary>
public class MoneyTransactionRequestValidatorTests
{
    /// <summary>
    /// The validator under test
    /// </summary>
    private readonly MoneyTransactionRequestValidator _validator = new();

    /// <summary>
    /// Validate: valid deposit request; no errors
    /// </summary>
    [Fact]
    public void Validate_ValidDepositRequest_NoErrors()
    {
        // Arrange
        var request = new MoneyTransactionRequest { Amount = 1000, IsDeposit = true };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Validate: valid withdrawal request; no errors
    /// </summary>
    [Fact]
    public void Validate_ValidWithdrawalRequest_NoErrors()
    {
        // Arrange
        var request = new MoneyTransactionRequest { Amount = 500, IsDeposit = false };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Validate: invalid amount; has error
    /// </summary>
    /// <param name="amount">Amount</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_InvalidAmount_HasError(decimal amount)
    {
        // Arrange
        var request = new MoneyTransactionRequest { Amount = amount, IsDeposit = true };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    /// <summary>
    /// Validate: positive amount; no error
    /// </summary>
    [Fact]
    public void Validate_PositiveAmount_NoError()
    {
        // Arrange
        var request = new MoneyTransactionRequest { Amount = 100, IsDeposit = false };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }
}

