using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BottleGame.UnitTests.Validators;

/// <summary>
/// Тесты для MoneyTransactionRequestValidator
/// </summary>
public class MoneyTransactionRequestValidatorTests
{
    private readonly MoneyTransactionRequestValidator _validator = new();

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

