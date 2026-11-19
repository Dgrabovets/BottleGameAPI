using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Validator for the application balance threshold update request
    /// </summary>
    public class UpdateAppBalanceThresholdRequestValidator : AbstractValidator<UpdateAppBalanceThresholdRequest>
    {
        /// <summary>
        /// Sets up the validation rules
        /// </summary>
        public UpdateAppBalanceThresholdRequestValidator()
        {
            RuleFor(x => x.LowerThreshold)
                .GreaterThanOrEqualTo(0).WithMessage("Lower threshold cannot be negative")
                .LessThanOrEqualTo(100_000_000).WithMessage("Lower threshold cannot exceed 100 000 000");
        }
    }
}
