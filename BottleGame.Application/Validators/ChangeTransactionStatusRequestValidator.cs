using BottleGame.Domain.Constants;
using BottleGame.Application.DTOs.Requests;
using FluentValidation;

namespace BottleGame.Application.Validators
{
    /// <summary>
    /// Валидатор для запроса изменения статуса транзакции
    /// </summary>
    public class ChangeTransactionStatusRequestValidator : AbstractValidator<ChangeTransactionStatusRequest>
    {
        public ChangeTransactionStatusRequestValidator()
        {
            RuleFor(x => x.TransactionStatusId)
                .Must(status => status == TransactionStatuses.Approved || status == TransactionStatuses.Rejected)
                .WithMessage($"Допустимые статусы: {TransactionStatuses.Approved} (Одобрено), {TransactionStatuses.Rejected} (Отклонено)");
        }
    }
}


