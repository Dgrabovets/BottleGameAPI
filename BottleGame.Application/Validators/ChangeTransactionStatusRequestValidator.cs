using BottleGame.Application.DTOs.Requests;
using BottleGame.Domain.Constants;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Validator for the transaction status change request
    /// </summary>
    public class ChangeTransactionStatusRequestValidator : AbstractValidator<ChangeTransactionStatusRequest>
    {
        /// <summary>
        /// Sets up the validation rules
        /// </summary>
        public ChangeTransactionStatusRequestValidator()
        {
            RuleFor(x => x.TransactionStatusId)
                .Must(status => status == TransactionStatuses.Approved || status == TransactionStatuses.Rejected)
                .WithMessage($"Allowed statuses: {TransactionStatuses.Approved} (Approved), {TransactionStatuses.Rejected} (Rejected)");
        }
    }
}
