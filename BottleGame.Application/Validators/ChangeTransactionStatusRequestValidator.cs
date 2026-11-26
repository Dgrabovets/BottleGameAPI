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
                .Must(status => status == TransactionStatuses.APPROVED || status == TransactionStatuses.REJECTED)
                .WithMessage($"Allowed statuses: {TransactionStatuses.APPROVED} (Approved), {TransactionStatuses.REJECTED} (Rejected)");
        }
    }
}
