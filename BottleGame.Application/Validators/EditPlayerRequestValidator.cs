using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Validator for the player edit request.
    /// Basic validation of the field format. The allowed values of
    /// currencies and payment methods are checked in the service through the processing API
    /// </summary>
    public class EditPlayerRequestValidator : AbstractValidator<EditPlayerRequest>
    {
        /// <summary>
        /// Sets up the validation rules
        /// </summary>
        public EditPlayerRequestValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Name));

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(500).WithMessage("Avatar URL must not exceed 500 characters")
                .Must(BeValidUrlOrEmpty).WithMessage("Invalid avatar URL format")
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

            RuleFor(x => x.WinChance)
                .InclusiveBetween(0f, 1f).WithMessage("Win chance must be between 0 and 1")
                .When(x => x.WinChance.HasValue);

            // Basic validation of the currency format (length and format)
            RuleFor(x => x.Currency)
                .MaximumLength(10).WithMessage("Currency code must not exceed 10 characters")
                .Matches(@"^[a-zA-Z_]+$").WithMessage("Currency code must contain only Latin letters and underscores")
                .When(x => !string.IsNullOrWhiteSpace(x.Currency));

            // Basic validation of the payment method format
            RuleFor(x => x.PaymentMethod)
                .MaximumLength(50).WithMessage("Payment method code must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Payment method code must contain only Latin letters, digits and underscores")
                .When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod));

            // Basic validation of the payment detail type format
            RuleFor(x => x.PaymentDetailType)
                .MaximumLength(50).WithMessage("Detail type must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Detail type must contain only Latin letters, digits and underscores")
                .When(x => !string.IsNullOrWhiteSpace(x.PaymentDetailType));

            // Basic validation of the payout method format
            RuleFor(x => x.PayoutMethod)
                .MaximumLength(50).WithMessage("Payout method code must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Payout method code must contain only Latin letters, digits and underscores")
                .When(x => !string.IsNullOrWhiteSpace(x.PayoutMethod));

            RuleFor(x => x.Wallet)
                .MaximumLength(100).WithMessage("Wallet must not exceed 100 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Wallet));

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0).WithMessage("Balance cannot be negative")
                .When(x => x.Balance.HasValue);

            // Check that Currency and PaymentMethod come as a pair
            RuleFor(x => x)
                .Must(x =>
                    (string.IsNullOrWhiteSpace(x.Currency) && string.IsNullOrWhiteSpace(x.PaymentMethod)) ||
                    (!string.IsNullOrWhiteSpace(x.Currency) && !string.IsNullOrWhiteSpace(x.PaymentMethod)))
                .WithMessage("Currency and PaymentMethod must be changed together");

            // Check that Wallet and PayoutMethod come as a pair
            RuleFor(x => x)
                .Must(x =>
                    (string.IsNullOrWhiteSpace(x.Wallet) && string.IsNullOrWhiteSpace(x.PayoutMethod)) ||
                    (!string.IsNullOrWhiteSpace(x.Wallet) && !string.IsNullOrWhiteSpace(x.PayoutMethod)))
                .WithMessage("Wallet and PayoutMethod must be changed together");
        }

        /// <summary>
        /// Checks that the value is empty or an absolute http(s) URL
        /// </summary>
        /// <param name="url">Value to check</param>
        /// <returns>true if the value is valid</returns>
        private static bool BeValidUrlOrEmpty(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true;
            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
    }
}


