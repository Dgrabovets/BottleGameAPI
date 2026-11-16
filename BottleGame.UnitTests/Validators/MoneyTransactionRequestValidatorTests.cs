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
    /// A deposit inside the allowed amount range passes the validation
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
    /// A withdrawal inside the allowed amount range passes the validation
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
    /// An amount below the minimum or above the maximum is rejected
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
    /// Amounts inside the allowed range pass the validation
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

