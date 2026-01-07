using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Валидатор для запроса завершения раунда
    /// </summary>
    public class FinishRoundRequestValidator : AbstractValidator<FinishRoundRequest>
    {
        public FinishRoundRequestValidator()
        {
            RuleFor(x => x.Bet)
                .GreaterThan(0).WithMessage("Ставка должна быть положительной")
                .LessThanOrEqualTo(1_000_000).WithMessage("Ставка не может превышать 1 000 000");
        }
    }
}


