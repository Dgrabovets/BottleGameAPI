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
    public class LiveTransactionProcessor : ITransactionProcessor
    {
        #region Fields

        /// <summary>
        /// Processing error that marks a pending withdrawal whose payout may already exist at the provider.
        /// Such a withdrawal is resolved by the payout notification or by approving it again, never by a manual rejection or a ban.
        /// The text is stored in the database and compared exactly, so it must never change
        /// </summary>
        public const string PayoutOutcomeUnknown = "Payout outcome unknown: wait for the YooKassa notification or approve again to repeat the request with the same idempotence key";

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<LiveTransactionProcessor> _logger;

        /// <summary>
        /// Payment provider
        /// </summary>
        private readonly IPaymentProvider _paymentProvider;

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

        /// <summary>
        /// Initializes a new instance of <see cref="LiveTransactionProcessor"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger</param>
        /// <param name="paymentProvider">Payment provider</param>
        public LiveTransactionProcessor(ApplicationDbContext context, ILogger<LiveTransactionProcessor> logger, IPaymentProvider paymentProvider)
        {
            _context = context;
            _logger = logger;
            _paymentProvider = paymentProvider;
        }

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
            // A marked row has already been sent once. The checks below only guard the first request: a deposit or a new threshold
            // in between must not strand a payout that may already exist
            var isRepeat = transaction.ProcessingError == PayoutOutcomeUnknown;

            if (!isRepeat)
            {
                var check = await CheckFirstPayoutAsync(transaction, gameParams, appBalance);
                if (check.IsFailure)
                {
                    return check;
                }
            }

            var playerOptions = transaction.Player.PlayerOptions!;
            var currency = playerOptions.Currency ?? DefaultCurrency;

            // The mark is saved before the request. A crash, a failed final save or a concurrent reject then cannot hide a payout
            // that really happened: a reject that read the row earlier loses on the row version, a later one sees the mark
            transaction.PaymentGateway = playerOptions.PayoutMethod;
            transaction.Currency = currency;
            transaction.ProcessingError = PayoutOutcomeUnknown;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("{Prefix} Transaction {TransactionId} was changed by another request before the payout", Prefix, transaction.Id);
                return Result.Conflict("The transaction was already changed by another request");
            }

            // The transaction id is the idempotence key at the provider, so a repeat asks for the same payout.
            // shortcut: YooKassa keeps a key for a limited time (24 hours by its documentation) and the time of the first request
            // is not stored, so a repeat after that window could make a second payout; storing it needs a schema change
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
                // A timeout, a network error or a 5xx answer: the payout may already exist, so the mark stays and the money is not returned
                _logger.LogError(ex, "{Prefix} Outcome of the payout for transaction {TransactionId} is unknown, the withdrawal stays pending", Prefix, transaction.Id);
                return Result.InternalError(PayoutOutcomeUnknown);
            }

            if (!payout.IsSuccess && isRepeat)
            {
                // A refused repeat only proves that the repeat was refused (for example, the wallet changed since the first request),
                // the first request may still have made the payout, so the mark stays
                _logger.LogError("{Prefix} The repeated payout for transaction {TransactionId} was refused: {ErrorMessage}. The withdrawal stays pending", Prefix, transaction.Id, payout.Error);
                return Result.InternalError(PayoutOutcomeUnknown);
            }

            if (!payout.IsSuccess)
            {
                // A 4xx answer to the first request: no payout was made, the mark is replaced by the error and the money is returned
                await RefundFailedWithdrawalAsync(transaction, payout.Error ?? "The payout was refused");

                _logger.LogError("{Prefix} Error while creating a payout for player {PlayerId}: {ErrorMessage}", Prefix, transaction.PlayerId, payout.Error);
                return Result.InternalError("Error while creating the payout:" + payout.Error);
            }

            transaction.ProcessingOrderId = payout.PayoutId;
            transaction.ProcessingError = null;
            transaction.TransactionStatusId = TransactionStatuses.Approved;

            return Result.Success();
        }

        /// <summary>
        /// Checks the conditions of the first payout request: enough played rounds and the payout limit
        /// </summary>
        /// <param name="transaction">Withdrawal transaction</param>
        /// <param name="gameParams">Game parameters</param>
        /// <param name="appBalance">Application balance</param>
        /// <returns>Success or the reason for the refusal</returns>
        private async Task<Result> CheckFirstPayoutAsync(MoneyTransaction transaction, GameParams gameParams, AppBalance appBalance)
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

            return Result.Success();
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
