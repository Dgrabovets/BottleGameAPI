using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Валидатор для учетных данных административных ролей
    /// </summary>
    public class AdministrationRoleUserCredentialsValidator : AbstractValidator<AdministrationRoleUserCredentials>
    {
        public AdministrationRoleUserCredentialsValidator()
        {
            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("Логин обязателен")
                .MaximumLength(50).WithMessage("Логин не должен превышать 50 символов");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен")
                .MinimumLength(6).WithMessage("Пароль должен содержать не менее 6 символов")
                .MaximumLength(100).WithMessage("Пароль не должен превышать 100 символов");
        }
    }
}


