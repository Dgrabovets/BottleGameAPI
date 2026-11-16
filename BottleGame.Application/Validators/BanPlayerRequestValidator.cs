using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Validator for the ban player request
    /// </summary>
    public class BanPlayerRequestValidator : AbstractValidator<BanPlayerRequest>
    {
        /// <summary>
        /// Sets up the validation rules
        /// </summary>
        public BanPlayerRequestValidator()
        {
            RuleFor(x => x.PlayerId)
                .GreaterThan(0).WithMessage("Player id must be a positive number");

            RuleFor(x => x.BanReason)
                .NotEmpty().WithMessage("Ban reason is required")
                .MaximumLength(255).WithMessage("Ban reason must not exceed 255 characters");
        }
    }
}


