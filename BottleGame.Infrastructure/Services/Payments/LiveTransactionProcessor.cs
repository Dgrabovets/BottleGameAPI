using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services.Payments
{
    /// <summary>
    /// Live mode: payments and payouts go through the payment provider, the outcome arrives by notification
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="LiveTransactionProcessor"/>
    /// </remarks>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger</param>
    /// <param name="paymentProvider">Payment provider</param>
    public class LiveTransactionProcessor(
        ApplicationDbContext context,
        ILogger<LiveTransactionProcessor> logger,
        IPaymentProvider paymentProvider) : ITransactionProcessor
    {
        #region Fields

        /// <summary>
        /// Processing error that marks a pending withdrawal whose payout may already exist at the provider.
        /// Such a withdrawal is resolved by the payout notification or by approving it again, never by a manual rejection
        /// </summary>
        public const string PayoutOutcomeUnknown = "Payout outcome unknown: wait for the YooKassa notification or approve again to repeat the request with the same idempotence key";

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<LiveTransactionProcessor> _logger = logger;

        /// <summary>
        /// Payment provider
        /// </summary>
        private readonly IPaymentProvider _paymentProvider = paymentProvider;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[LiveTransactionProcessor]";

        /// <summary>
        /// Maximum share of the lower threshold of the application balance available for a single payout
        /// </summary>
        private const decimal MaxPayoutShareOfLowerThreshold = 0.1m;

        /// <summary>
        /// Default currency if the player has none set
        /// </summary>
        private const string DefaultCurrency = "rub";

        #endregion Fields

        #region Processing

        /// <inheritdoc/>
        public async Task<Result> CheckRequestAsync(Player player, bool isDeposit, GameParams gameParams)
        {
            var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == player.Id);

            if (roundsPlayedStat is null)
            {
                _logger.LogWarning("{Prefix} Statistics of the played rounds not found for player {PlayerId}", Prefix, player.Id);
                return Result.Failure("Statistics of the played rounds of the player not found");
            }

            if (!isDeposit && roundsPlayedStat.PlayedRoundsCount < gameParams.MinRoundsCountForWithdrawal)
            {
                _logger.LogWarning("{Prefix} Not enough played rounds to withdraw for player {PlayerId}", Prefix, player.Id);
                return Result.Failure($"To withdraw, you need to play at least {gameParams.MinRoundsCountForWithdrawal} rounds. Played so far: {roundsPlayedStat.PlayedRoundsCount}");
            }

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<object?>> StartDepositAsync(MoneyTransaction transaction, Player player)
        {
            try
            {
                _logger.LogInformation("{Prefix} Creating a payment request for player {PlayerId}, amount {Amount}", Prefix, player.Id, transaction.Amount);

                var currency = player.PlayerOptions?.Currency ?? DefaultCurrency;
                var paymentRequest = new CreatePaymentRequest(
                    transaction.Id,
                    transaction.Amount,
                    currency,
                    $"Balance top-up, transaction {transaction.Id}");

                var payment = await _paymentProvider.CreatePaymentAsync(paymentRequest);

                if (!payment.IsSuccess || string.IsNullOrEmpty(payment.PaymentUrl))
                {
                    await RejectFailedTransactionAsync(transaction, payment.Error);

                    _logger.LogError("{Prefix} Error while creating a payment for player {PlayerId}: {ErrorMessage}", Prefix, player.Id, payment.Error);
                    return Result<object?>.InternalError("Error while creating the payment:" + payment.Error);
                }

                transaction.ProcessingOrderId = payment.PaymentId;
                transaction.PaymentUrl = payment.PaymentUrl;
                transaction.PaymentGateway = player.PlayerOptions?.PaymentMethod;
                transaction.Currency = currency;
                await _context.SaveChangesAsync();

                return Result<object?>.Success(new
                {
                    PaymentLink = payment.PaymentUrl,
                    OrderId = payment.PaymentId
                });
            }
            catch (Exception ex)
            {
                await RejectFailedTransactionAsync(transaction, null);

                _logger.LogError(ex, "{Prefix} Error while creating a payment for player {PlayerId}", Prefix, player.Id);
                return Result<object?>.InternalError("Error while creating the payment");
            }
        }

        /// <inheritdoc/>
        public async Task<Result> ApproveWithdrawalAsync(MoneyTransaction transaction, GameParams gameParams, AppBalance appBalance)
        {
            var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == transaction.PlayerId);

            if (roundsPlayedStat is null)
            {
                return Result.Failure("Statistics of the played rounds of the player not found");
            }

            var minRoundsCount = gameParams.MinRoundsCountForWithdrawal;
            if (roundsPlayedStat.PlayedRoundsCount < minRoundsCount)
            {
                _logger.LogWarning("{Prefix} Not enough played rounds to withdraw for player {PlayerId}", Prefix, transaction.PlayerId);
                return Result.Failure($"To withdraw, you need to play at least {minRoundsCount} rounds. Played so far: {roundsPlayedStat.PlayedRoundsCount}");
            }

            if (transaction.Amount > appBalance.LowerThreshold * MaxPayoutShareOfLowerThreshold)
            {
                _logger.LogWarning("{Prefix} Transaction cannot be approved - the amount exceeds the threshold", Prefix);
                return Result.Failure("Transaction cannot be approved - the withdrawal amount exceeds the lower threshold of the application balance!");
            }

            var playerOptions = transaction.Player.PlayerOptions!;
            var currency = playerOptions.Currency ?? DefaultCurrency;

            // The transaction id is the idempotence key at the provider, so approving again after an unknown outcome repeats the same payout
            var payoutRequest = new CreatePayoutRequest(
                transaction.Id,
                transaction.Amount,
                currency,
                playerOptions.PayoutMethod,
                playerOptions.Wallet ?? string.Empty,
                $"Withdrawal, transaction {transaction.Id}");

            CreatePayoutResult payout;
            try
            {
                payout = await _paymentProvider.CreatePayoutAsync(payoutRequest);
            }
            catch (Exception ex)
            {
                // A timeout, a network error or a 5xx answer: the payout may already exist, so the money is not returned
                await MarkPayoutOutcomeUnknownAsync(transaction, playerOptions.PayoutMethod, currency);

                _logger.LogError(ex, "{Prefix} Outcome of the payout for transaction {TransactionId} is unknown, the withdrawal stays pending", Prefix, transaction.Id);
                return Result.InternalError(PayoutOutcomeUnknown);
            }

            // On a repeat a refusal only proves that the repeat was refused (for example, the wallet changed since the first request),
            // the first request may still have created the payout, so the money is not returned
            if (!payout.IsSuccess && transaction.ProcessingError == PayoutOutcomeUnknown)
            {
                _logger.LogError("{Prefix} The repeated payout for transaction {TransactionId} was refused: {ErrorMessage}. The withdrawal stays pending", Prefix, transaction.Id, payout.Error);
                return Result.InternalError(PayoutOutcomeUnknown);
            }

            if (!payout.IsSuccess)
            {
                await RefundFailedWithdrawalAsync(transaction, payout.Error);

                _logger.LogError("{Prefix} Error while creating a payout for player {PlayerId}: {ErrorMessage}", Prefix, transaction.PlayerId, payout.Error);
                return Result.InternalError("Error while creating the payout:" + payout.Error);
            }

            transaction.ProcessingOrderId = payout.PayoutId;
            transaction.PaymentGateway = playerOptions.PayoutMethod;
            transaction.Currency = currency;
            transaction.ProcessingError = null;
            transaction.TransactionStatusId = TransactionStatuses.Approved;

            return Result.Success();
        }

        /// <summary>
        /// Leaves a withdrawal pending with a mark that its payout may exist at the provider
        /// </summary>
        /// <param name="transaction">Withdrawal transaction</param>
        /// <param name="payoutMethod">Payout method the request was sent with</param>
        /// <param name="currency">Currency the request was sent with</param>
        private async Task MarkPayoutOutcomeUnknownAsync(MoneyTransaction transaction, string payoutMethod, string currency)
        {
            transaction.PaymentGateway = payoutMethod;
            transaction.Currency = currency;
            transaction.ProcessingError = PayoutOutcomeUnknown;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Rejects a deposit that could not be created at the payment provider
        /// </summary>
        /// <param name="transaction">Deposit transaction</param>
        /// <param name="error">Text of the processing error</param>
        private async Task RejectFailedTransactionAsync(MoneyTransaction transaction, string? error)
        {
            transaction.TransactionStatusId = TransactionStatuses.Rejected;
            transaction.IsProcessingSuccess = false;
            transaction.ProcessingError = error;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Rejects a withdrawal that could not be created at the payment provider and returns the funds to the player
        /// </summary>
        /// <param name="transaction">Withdrawal transaction</param>
        /// <param name="error">Text of the processing error</param>
        private async Task RefundFailedWithdrawalAsync(MoneyTransaction transaction, string? error)
        {
            transaction.Player.Balance.Amount += transaction.Amount;
            transaction.ProcessedAt = DateTime.UtcNow;
            await RejectFailedTransactionAsync(transaction, error);
        }

        #endregion Processing
    }
}
