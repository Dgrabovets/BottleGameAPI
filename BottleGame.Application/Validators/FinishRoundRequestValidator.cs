using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Validator for the finish round request
    /// </summary>
    public class FinishRoundRequestValidator : AbstractValidator<FinishRoundRequest>
    {
        /// <summary>
        /// Sets up the validation rules
        /// </summary>
        public FinishRoundRequestValidator()
        {
            RuleFor(x => x.Bet)
                .GreaterThan(0).WithMessage("Bet must be positive")
                .LessThanOrEqualTo(1_000_000).WithMessage("Bet cannot exceed 1 000 000");
        }
    }
}
