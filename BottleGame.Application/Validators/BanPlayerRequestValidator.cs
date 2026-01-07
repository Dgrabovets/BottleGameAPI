using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Валидатор для запроса бана игрока
    /// </summary>
    public class BanPlayerRequestValidator : AbstractValidator<BanPlayerRequest>
    {
        public BanPlayerRequestValidator()
        {
            RuleFor(x => x.PlayerId)
                .GreaterThan(0).WithMessage("ID игрока должен быть положительным числом");

            RuleFor(x => x.BanReason)
                .NotEmpty().WithMessage("Причина бана обязательна")
                .MaximumLength(255).WithMessage("Причина бана не должна превышать 255 символов");
        }
    }
}


