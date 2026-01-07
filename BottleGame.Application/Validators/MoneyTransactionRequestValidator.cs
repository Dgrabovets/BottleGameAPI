using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Валидатор для запроса денежной транзакции
    /// </summary>
    public class MoneyTransactionRequestValidator : AbstractValidator<MoneyTransactionRequest>
    {
        /// <summary>
        /// Минимальная сумма транзакции
        /// </summary>
        private const int MinAmount = 100;

        /// <summary>
        /// Максимальная сумма транзакции
        /// </summary>
        private const int MaxAmount = 1_000_000;

        public MoneyTransactionRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(MinAmount)
                .WithMessage($"Минимальная сумма транзакции: {MinAmount}")
                .LessThanOrEqualTo(MaxAmount)
                .WithMessage($"Максимальная сумма транзакции: {MaxAmount:N0}");
        }
    }
}


