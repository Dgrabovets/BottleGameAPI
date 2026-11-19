using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Validator for the application balance top-up request
    /// </summary>
    public class TopUpAppBalanceRequestValidator : AbstractValidator<TopUpAppBalanceRequest>
    {
        /// <summary>
        /// Sets up the validation rules
        /// </summary>
        public TopUpAppBalanceRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Top-up amount must be positive")
                .LessThanOrEqualTo(100_000_000).WithMessage("Top-up amount cannot exceed 100 000 000");
        }
    }
}
