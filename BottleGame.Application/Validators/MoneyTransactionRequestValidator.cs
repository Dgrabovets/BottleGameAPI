using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Validator for the money transaction request
    /// </summary>
    public class MoneyTransactionRequestValidator : AbstractValidator<MoneyTransactionRequest>
    {
        /// <summary>
        /// Minimum transaction amount
        /// </summary>
        private const int MinAmount = 100;

        /// <summary>
        /// Maximum transaction amount
        /// </summary>
        private const int MaxAmount = 1_000_000;

        /// <summary>
        /// Sets up the validation rules
        /// </summary>
        public MoneyTransactionRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(MinAmount)
                .WithMessage($"Minimum transaction amount: {MinAmount}")
                .LessThanOrEqualTo(MaxAmount)
                .WithMessage($"Maximum transaction amount: {MaxAmount:N0}");
        }
    }
}


