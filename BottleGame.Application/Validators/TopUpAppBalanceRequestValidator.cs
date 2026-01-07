using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Валидатор для запроса пополнения баланса приложения
    /// </summary>
    public class TopUpAppBalanceRequestValidator : AbstractValidator<TopUpAppBalanceRequest>
    {
        public TopUpAppBalanceRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Сумма пополнения должна быть положительной")
                .LessThanOrEqualTo(100_000_000).WithMessage("Сумма пополнения не может превышать 100 000 000");
        }
    }
}


