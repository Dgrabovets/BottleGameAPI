using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BottleGame.UnitTests.Validators;

/// <summary>
/// Тесты для AuthAdminRequestValidator
/// </summary>
public class AdministrationRoleUserCredentialsValidatorTests
{
    private readonly AdministrationRoleUserCredentialsValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        // Arrange
        var request = new AdministrationRoleUserCredentials { Login = "admin", Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyLogin_HasError(string? login)
    {
        // Arrange
        var request = new AdministrationRoleUserCredentials { Login = login!, Password = "password123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Login);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyPassword_HasError(string? password)
    {
        // Arrange
        var request = new AdministrationRoleUserCredentials { Login = "admin", Password = password! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShortPassword_HasError()
    {
        // Arrange
        var request = new AdministrationRoleUserCredentials { Login = "admin", Password = "123" };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

