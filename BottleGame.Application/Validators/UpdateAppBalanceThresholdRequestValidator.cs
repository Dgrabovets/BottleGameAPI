using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Валидатор для запроса обновления порога баланса приложения
    /// </summary>
    public class UpdateAppBalanceThresholdRequestValidator : AbstractValidator<UpdateAppBalanceThresholdRequest>
    {
        public UpdateAppBalanceThresholdRequestValidator()
        {
            RuleFor(x => x.LowerThreshold)
                .GreaterThanOrEqualTo(0).WithMessage("Нижний порог не может быть отрицательным")
                .LessThanOrEqualTo(100_000_000).WithMessage("Нижний порог не может превышать 100 000 000");
        }
    }
}


