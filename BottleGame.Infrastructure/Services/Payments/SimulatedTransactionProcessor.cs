using BottleGame.Application.Common;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services.Payments
{
    /// <summary>
    /// Simulated mode: deposits and withdrawals complete locally without a payment provider and real money
    /// </summary>
    public class SimulatedTransactionProcessor : ITransactionProcessor
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<SimulatedTransactionProcessor> _logger;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="SimulatedTransactionProcessor"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger</param>
        public SimulatedTransactionProcessor(ApplicationDbContext context, ILogger<SimulatedTransactionProcessor> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Processing

        /// <inheritdoc/>
        public Task<Result> CheckRequestAsync(Player player, bool isDeposit, GameParams gameParams) =>
            Task.FromResult(Result.Success());

        /// <inheritdoc/>
        public async Task<Result<object?>> StartDepositAsync(MoneyTransaction transaction, Player player) =>
            Result<object?>.Success(await CompleteDepositLocallyAsync(transaction, player, transaction.Amount));

        /// <inheritdoc/>
        public Task<Result> ApproveWithdrawalAsync(MoneyTransaction transaction, GameParams gameParams, AppBalance appBalance)
        {
            CompleteWithdrawalLocally(transaction, appBalance, TransactionStatuses.APPROVED);
            _logger.LogInformation("Withdrawal {TransactionId} approved locally without processing", transaction.Id);
            return Task.FromResult(Result.Success());
        }

        /// <summary>
        /// Completes a deposit locally without calling the processing platform
        /// </summary>
        /// <param name="transaction">Deposit transaction</param>
        /// <param name="player">Player</param>
        /// <param name="amount">Deposit amount</param>
        /// <returns>API response with the order identifier</returns>
        private async Task<object> CompleteDepositLocallyAsync(
            MoneyTransaction transaction,
            Player player,
            decimal amount)
        {
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Application balance not found");

            transaction.TransactionStatusId = TransactionStatuses.APPROVED;
            transaction.IsProcessingSuccess = true;
            transaction.ProcessedAt = DateTime.UtcNow;
            transaction.ProcessingOrderId = $"test-deposit-{transaction.Id}";
            transaction.PaymentGateway = player.PlayerOptions!.PaymentMethod.ToString();
            transaction.Currency = player.PlayerOptions.Currency?.ToString() ?? "rub";

            player.Balance!.Amount += amount;
            appBalance.Amount += amount;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[Simulated] Deposit {TransactionId} completed locally without processing",
                transaction.Id);

            return new
            {
                PaymentLink = (string?)null,
                OrderId = transaction.ProcessingOrderId
            };
        }

        /// <summary>
        /// Completes a withdrawal locally without calling the processing platform
        /// </summary>
        /// <param name="transaction">Withdrawal transaction</param>
        /// <param name="appBalance">Application balance</param>
        /// <param name="approvedStatusId">Identifier of the "Approved" status</param>
        private void CompleteWithdrawalLocally(
            MoneyTransaction transaction,
            AppBalance appBalance,
            int approvedStatusId)
        {
            transaction.TransactionStatusId = approvedStatusId;
            transaction.IsProcessingSuccess = true;
            transaction.ProcessedAt = DateTime.UtcNow;
            transaction.ProcessingOrderId = $"test-payout-{transaction.Id}";
            transaction.PaymentGateway = transaction.Player!.PlayerOptions!.PayoutMethod.ToString();
            transaction.Currency = transaction.Player.PlayerOptions.Currency?.ToString() ?? "rub";
            appBalance.Amount -= transaction.Amount;
        }

        #endregion Processing
    }
}
