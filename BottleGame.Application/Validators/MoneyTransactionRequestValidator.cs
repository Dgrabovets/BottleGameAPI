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
        private const int MIN_AMOUNT = 100;

        /// <summary>
        /// Maximum transaction amount
        /// </summary>
        private const int MAX_AMOUNT = 1_000_000;

        /// <summary>
        /// Sets up the validation rules
        /// </summary>
        public MoneyTransactionRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(MIN_AMOUNT)
                .WithMessage($"Minimum transaction amount: {MIN_AMOUNT}")
                .LessThanOrEqualTo(MAX_AMOUNT)
                .WithMessage($"Maximum transaction amount: {MAX_AMOUNT:N0}");
        }
    }
}
