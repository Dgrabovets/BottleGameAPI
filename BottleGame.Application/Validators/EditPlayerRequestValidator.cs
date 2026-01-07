using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Валидатор для запроса редактирования игрока.
    /// Базовая валидация формата полей. Проверка допустимых значений
    /// валют и платёжных методов выполняется в сервисе через API процессинга.
    /// </summary>
    public class EditPlayerRequestValidator : AbstractValidator<EditPlayerRequest>
    {
        public EditPlayerRequestValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Имя не должно превышать 100 символов")
                .When(x => !string.IsNullOrWhiteSpace(x.Name));

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(500).WithMessage("URL аватара не должен превышать 500 символов")
                .Must(BeValidUrlOrEmpty).WithMessage("Некорректный формат URL аватара")
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

            RuleFor(x => x.WinChance)
                .InclusiveBetween(0f, 1f).WithMessage("Шанс выигрыша должен быть от 0 до 1")
                .When(x => x.WinChance.HasValue);

            // Базовая валидация формата валюты (длина и формат)
            RuleFor(x => x.Currency)
                .MaximumLength(10).WithMessage("Код валюты не должен превышать 10 символов")
                .Matches(@"^[a-zA-Z_]+$").WithMessage("Код валюты должен содержать только латинские буквы и подчёркивания")
                .When(x => !string.IsNullOrWhiteSpace(x.Currency));

            // Базовая валидация формата метода оплаты
            RuleFor(x => x.PaymentMethod)
                .MaximumLength(50).WithMessage("Код метода оплаты не должен превышать 50 символов")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Код метода оплаты должен содержать только латинские буквы, цифры и подчёркивания")
                .When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod));

            // Базовая валидация формата метода выплаты
            RuleFor(x => x.PayoutMethod)
                .MaximumLength(50).WithMessage("Код метода выплаты не должен превышать 50 символов")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Код метода выплаты должен содержать только латинские буквы, цифры и подчёркивания")
                .When(x => !string.IsNullOrWhiteSpace(x.PayoutMethod));

            RuleFor(x => x.Wallet)
                .MaximumLength(100).WithMessage("Кошелёк не должен превышать 100 символов")
                .When(x => !string.IsNullOrWhiteSpace(x.Wallet));

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0).WithMessage("Баланс не может быть отрицательным")
                .When(x => x.Balance.HasValue);

            // Проверка на парность Currency и PaymentMethod
            RuleFor(x => x)
                .Must(x =>
                    (string.IsNullOrWhiteSpace(x.Currency) && string.IsNullOrWhiteSpace(x.PaymentMethod)) ||
                    (!string.IsNullOrWhiteSpace(x.Currency) && !string.IsNullOrWhiteSpace(x.PaymentMethod)))
                .WithMessage("Поля Currency и PaymentMethod должны изменяться вместе");

            // Проверка на парность Wallet и PayoutMethod
            RuleFor(x => x)
                .Must(x =>
                    (string.IsNullOrWhiteSpace(x.Wallet) && string.IsNullOrWhiteSpace(x.PayoutMethod)) ||
                    (!string.IsNullOrWhiteSpace(x.Wallet) && !string.IsNullOrWhiteSpace(x.PayoutMethod)))
                .WithMessage("Поля Wallet и PayoutMethod должны изменяться вместе");
        }

        private static bool BeValidUrlOrEmpty(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true;
            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
    }
}


