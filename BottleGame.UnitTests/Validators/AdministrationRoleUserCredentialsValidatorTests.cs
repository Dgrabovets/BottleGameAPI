using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Validators;
using FluentValidation.TestHelper;

namespace BottleGame.UnitTests.Validators;

/// <summary>
/// Tests for AuthAdminRequestValidator
/// </summary>
public class AdministrationRoleUserCredentialsValidatorTests
{
    /// <summary>
    /// The validator under test
    /// </summary>
    private readonly AdministrationRoleUserCredentialsValidator _validator = new();

    /// <summary>
    /// A login and a password of allowed length pass the validation
    /// </summary>
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

    /// <summary>
    /// An empty login is rejected
    /// </summary>
    /// <param name="login">Login</param>
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

    /// <summary>
    /// An empty password is rejected
    /// </summary>
    /// <param name="password">Password</param>
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

    /// <summary>
    /// A password shorter than 6 characters is rejected
    /// </summary>
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

