using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Validator for the credentials of administration roles
    /// </summary>
    public class AdministrationRoleUserCredentialsValidator : AbstractValidator<AdministrationRoleUserCredentials>
    {
        /// <summary>
        /// Sets up the validation rules
        /// </summary>
        public AdministrationRoleUserCredentialsValidator()
        {
            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("Login is required")
                .MaximumLength(50).WithMessage("Login must not exceed 50 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters");
        }
    }
}


