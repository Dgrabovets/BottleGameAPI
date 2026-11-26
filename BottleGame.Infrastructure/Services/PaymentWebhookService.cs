using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.External.YooKassa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Service for handling YooKassa notifications about payments and payouts
    /// </summary>
    /// <remarks>
    /// YooKassa notifications are not signed, so only the object identifier is taken from the body.
    /// The status, the amount and the link to the transaction are read from the YooKassa API response to a request by this identifier.
    /// Initializes a new instance of <see cref="PaymentWebhookService"/>
    /// </remarks>
    /// <param name="context">Database context</param>
    /// <param name="paymentProvider">Payment provider</param>
    /// <param name="logger">Logger</param>
    public class PaymentWebhookService(
        ApplicationDbContext context,
        IPaymentProvider paymentProvider,
        ILogger<PaymentWebhookService> logger) : IPaymentWebhookService
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Payment provider
        /// </summary>
        private readonly IPaymentProvider _paymentProvider = paymentProvider;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<PaymentWebhookService> _logger = logger;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[Webhook]";

        /// <summary>
        /// Share of the referral bonus from the deposit amount of the invited player
        /// </summary>
        private const decimal ReferralBonusRate = 0.1m;

        /// <summary>
        /// JSON deserialization settings
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// How much of a body that is not a notification is stored in the webhook log
        /// </summary>
        private const int MaxUnparsedBodyLength = 200;

        #endregion Fields

        #region Public Methods

        /// <inheritdoc/>
        public async Task<Result> HandleNotificationAsync(WebhookRequestInfo request)
        {
            var webhookLog = CreateWebhookLog(request);

            try
            {
                var notification = TryParseNotification(request.Body);
                if (notification?.Object is null || string.IsNullOrWhiteSpace(notification.Object.Id))
                {
                    // Anyone can post here, so a body that is not a notification is kept only as a short prefix
                    webhookLog.RawRequest = request.Body.Length <= MaxUnparsedBodyLength ? request.Body : request.Body[..MaxUnparsedBodyLength];
                    return await LogAndFailAsync(webhookLog, $"Failed to parse the notification ({request.Body.Length} characters)");
                }

                _logger.LogInformation("{Prefix} Notification received: {Event} {ObjectId}", Prefix, LogSanitizer.Clean(notification.Event), LogSanitizer.Clean(notification.Object.Id));

                webhookLog.RawRequest = request.Body;
                webhookLog.WebhookType = notification.Event;
                webhookLog.OrderId = notification.Object.Id;

                return notification.Event switch
                {
                    "payment.succeeded" or "payment.canceled" => await HandleAsync(webhookLog, notification.Object.Id, isPayout: false),
                    "payout.succeeded" or "payout.canceled" => await HandleAsync(webhookLog, notification.Object.Id, isPayout: true),
                    _ => await SaveLogAsync(webhookLog, "Ignored", Result.Success())
                };
            }
            catch (Exception ex)
            {
                webhookLog.ErrorMessage = ex.ToString();
                _logger.LogError(ex, "{Prefix} Error while handling the notification", Prefix);
                await SaveLogAsync(webhookLog, "Error", Result.InternalError());
                return Result.InternalError();
            }
        }

        #endregion Public Methods

        #region Webhook Pipeline

        /// <summary>
        /// Verifies the object at the provider, finds the transaction and applies the outcome once
        /// </summary>
        /// <param name="webhookLog">Log record</param>
        /// <param name="providerId">Identifier of the payment or payout at the provider</param>
        /// <param name="isPayout">true for a payout, false for a payment</param>
        /// <returns>Result of the handling</returns>
        private async Task<Result> HandleAsync(ProcessingWebhookLog webhookLog, string providerId, bool isPayout)
        {
            var operation = isPayout
                              ? await _paymentProvider.GetPayoutAsync(providerId)
                              : await _paymentProvider.GetPaymentAsync(providerId);

            if (operation is null)
            {
                return await LogAndFailAsync(webhookLog, $"Object not found at the provider: {providerId}");
            }

            // The object was received directly from the provider, so the notification is not forged.
            webhookLog.IsSignatureValid = true;
            webhookLog.Result = operation.Status.ToString();

            if (operation.TransactionId is not { } transactionId)
            {
                return await LogAndFailAsync(webhookLog, $"Object {providerId} has no transaction identifier");
            }

            webhookLog.TransactionId = transactionId.ToString();

            var transaction = await _context.MoneyTransactions
                .Include(t => t.Player).ThenInclude(p => p.Balance)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction is null)
            {
                return await LogAndFailAsync(webhookLog, $"Transaction not found: {transactionId}");
            }

            var expectedType = isPayout ? TransactionTypes.Withdrawal : TransactionTypes.Deposit;
            if (transaction.TransactionTypeId != expectedType)
            {
                return await LogAndFailAsync(webhookLog, $"Type of transaction {transactionId} does not match the event");
            }

            // The provider repeats the delivery; the outcome of the transaction is recorded once,
            // otherwise the money would be credited or returned twice.
            if (transaction.IsProcessingSuccess is not null)
            {
                _logger.LogWarning("{Prefix} Repeated notification for transaction {TransactionId} ignored", Prefix, transaction.Id);
                return await SaveLogAsync(webhookLog, "Duplicate", Result.Success());
            }

            if (operation.Amount != transaction.Amount)
            {
                return await LogAndFailAsync(webhookLog, $"Amount at the provider ({operation.Amount}) does not match the transaction amount ({transaction.Amount})");
            }

            Result result;
            try
            {
                result = (isPayout, operation.Status) switch
                {
                    (false, ProviderOperationStatus.Succeeded) => await ProcessSuccessfulPaymentAsync(transaction, operation),
                    (false, ProviderOperationStatus.Canceled) => await ProcessFailedPaymentAsync(transaction),
                    (true, ProviderOperationStatus.Succeeded) => await ProcessSuccessfulPayoutAsync(transaction, operation),
                    (true, ProviderOperationStatus.Canceled) => await ProcessFailedPayoutAsync(transaction, operation),
                    _ => Result.Success() // the operation is not finished yet
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                // Either a concurrent handler has already applied this notification, or something else changed the
                // balance at the same moment. Only the first case is a duplicate: in the second one nothing was applied
                // and the error makes the provider deliver the notification again.
                _context.ChangeTracker.Clear();
                var alreadyHandled = await _context.MoneyTransactions
                    .AsNoTracking()
                    .AnyAsync(t => t.Id == transactionId && t.IsProcessingSuccess != null);

                if (alreadyHandled)
                {
                    _logger.LogWarning("{Prefix} Transaction {TransactionId} was handled by a concurrent request, the repeat is ignored", Prefix, transactionId);
                    return await SaveLogAsync(webhookLog, "Duplicate", Result.Success());
                }

                _logger.LogWarning("{Prefix} Concurrent update while handling transaction {TransactionId}, the provider will retry", Prefix, transactionId);
                return await LogAndFailAsync(webhookLog, "Concurrent update, try again");
            }

            return result.IsSuccess
                ? await SaveLogAsync(webhookLog, "Success", result)
                : await LogAndFailAsync(webhookLog, result.Error ?? "Handling error");
        }

        /// <summary>
        /// Parses the notification body
        /// </summary>
        /// <param name="body">Raw request body</param>
        /// <returns>The notification or null if the body is not valid JSON</returns>
        private static YooKassaNotification? TryParseNotification(string body)
        {
            try
            {
                return JsonSerializer.Deserialize<YooKassaNotification>(body, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        #endregion Webhook Pipeline

        #region Payment Processing

        /// <summary>
        /// Handles a successful payment (deposit)
        /// </summary>
        /// <param name="transaction">Transaction</param>
        /// <param name="payment">Payment at the provider</param>
        /// <returns>Result of the handling</returns>
        private async Task<Result> ProcessSuccessfulPaymentAsync(MoneyTransaction transaction, ProviderOperation payment)
        {
            var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == transaction.PlayerId);

            if (roundsPlayedStat is null)
            {
                _logger.LogWarning("{Prefix} Statistics of the played rounds not found for player {PlayerId}", Prefix, transaction.PlayerId);
                return Result.Failure("Statistics of the played rounds of the player not found");
            }

            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Application balance not found", Prefix);
                return Result.Failure("Application balance not found");
            }

            transaction.IsProcessingSuccess = true;
            transaction.TransactionStatusId = TransactionStatuses.Approved;
            transaction.ProcessingOrderId = payment.Id;
            transaction.Currency = payment.Currency;

            transaction.Player.Balance.Amount += transaction.Amount;
            appBalance.Amount += transaction.Amount;

            ResetPlayedRounds(roundsPlayedStat, transaction);

            await ProcessReferralBonusAsync(transaction);

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Transaction {TransactionId} handled successfully", Prefix, transaction.Id);
            return Result.Success();
        }

        /// <summary>
        /// Handles a failed payment (deposit)
        /// </summary>
        /// <param name="transaction">Transaction</param>
        /// <returns>Result of the handling</returns>
        private async Task<Result> ProcessFailedPaymentAsync(MoneyTransaction transaction)
        {
            transaction.IsProcessingSuccess = false;
            transaction.TransactionStatusId = TransactionStatuses.Rejected;
            transaction.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Transaction {TransactionId} finished with an error", Prefix, transaction.Id);
            return Result.Success();
        }

        /// <summary>
        /// Handles a successful payout
        /// </summary>
        /// <param name="transaction">Transaction</param>
        /// <param name="payout">Payout at the provider</param>
        /// <returns>Result of the handling</returns>
        private async Task<Result> ProcessSuccessfulPayoutAsync(MoneyTransaction transaction, ProviderOperation payout)
        {
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Application balance not found", Prefix);
                return Result.Failure("Application balance not found");
            }

            transaction.IsProcessingSuccess = true;
            transaction.TransactionStatusId = TransactionStatuses.Approved;
            // The id is missing when creating the payout timed out, the notification is then the first to tell it
            transaction.ProcessingOrderId = payout.Id;
            transaction.ProcessingError = null;
            appBalance.Amount -= transaction.Amount;

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Payout {TransactionId} handled successfully", Prefix, transaction.Id);
            return Result.Success();
        }

        /// <summary>
        /// Handles a canceled payout and returns the funds to the player
        /// </summary>
        /// <param name="transaction">Transaction</param>
        /// <param name="payout">Payout at the provider</param>
        /// <returns>Result of the handling</returns>
        private async Task<Result> ProcessFailedPayoutAsync(MoneyTransaction transaction, ProviderOperation payout)
        {
            var reason = payout.CancellationReason;
            transaction.IsProcessingSuccess = false;
            transaction.TransactionStatusId = TransactionStatuses.Rejected;
            transaction.ProcessingOrderId = payout.Id;
            transaction.ProcessingError = reason;
            transaction.Player.Balance.Amount += transaction.Amount;

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogWarning("{Prefix} Payout {TransactionId} canceled: {Reason}", Prefix, transaction.Id, reason);
            return Result.Success();
        }

        /// <summary>
        /// Credits the referral bonus to the inviting player if they are not banned
        /// </summary>
        /// <param name="transaction">Deposit transaction of the invited player</param>
        private async Task ProcessReferralBonusAsync(MoneyTransaction transaction)
        {
            var invitation = await _context.InvitedPlayers
                .Include(ip => ip.Inviter).ThenInclude(p => p.Balance)
                .Include(ip => ip.Inviter).ThenInclude(p => p.Ban)
                .FirstOrDefaultAsync(ip => ip.PlayerIdInvited == transaction.PlayerId);

            var referrer = invitation?.Inviter;
            if (referrer is null || referrer.Ban is { IsActive: true })
            {
                return;
            }

            var bonusAmount = (int)Math.Round(transaction.Amount * ReferralBonusRate, MidpointRounding.AwayFromZero);
            referrer.Balance.Amount += bonusAmount;

            var referrerRoundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == referrer.Id);

            if (referrerRoundsPlayedStat is not null)
            {
                ResetPlayedRounds(referrerRoundsPlayedStat, transaction);
            }

            _logger.LogInformation(
                "{Prefix} Credited {BonusAmount} to referrer {ReferrerId} for the deposit of invited player {PlayerId}",
                Prefix, bonusAmount, referrer.Id, transaction.PlayerId);
        }

        /// <summary>
        /// Resets the counter of played rounds after a deposit
        /// </summary>
        /// <param name="statistic">Round statistics of the player</param>
        /// <param name="depositTransaction">Deposit transaction</param>
        private static void ResetPlayedRounds(PlayerPlayedRoundsStatisticForWithdrawal statistic, MoneyTransaction depositTransaction)
        {
            statistic.MoneyTransaction = depositTransaction;
            statistic.DepositTransactionId = depositTransaction.Id;
            statistic.PlayedRoundsCount = 0;
        }

        #endregion Payment Processing

        #region Webhook Log

        /// <summary>
        /// Creates a log record for an incoming request
        /// </summary>
        /// <param name="request">Incoming webhook request</param>
        /// <returns>Log record</returns>
        private static ProcessingWebhookLog CreateWebhookLog(WebhookRequestInfo request) => new()
        {
            HttpMethod = request.HttpMethod,
            ContentType = request.ContentType,
            IpAddress = request.IpAddress ?? "Unknown",
            UserAgent = request.UserAgent,
            ReceivedAt = DateTime.UtcNow,
            WebhookType = "yookassa"
        };

        /// <summary>
        /// Saves the log record with the final status
        /// </summary>
        /// <param name="log">Log record</param>
        /// <param name="processingStatus">Final handling status</param>
        /// <param name="result">Result returned to the caller</param>
        /// <returns>The passed result</returns>
        private async Task<Result> SaveLogAsync(ProcessingWebhookLog log, string processingStatus, Result result)
        {
            log.ProcessingStatus = processingStatus;
            log.ProcessedAt = DateTime.UtcNow;

            try
            {
                _context.ProcessingWebhookLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error while saving the webhook log", Prefix);
            }

            return result;
        }

        /// <summary>
        /// Writes the error to the log and returns a failed result
        /// </summary>
        /// <param name="log">Log record</param>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Result with code 400</returns>
        private async Task<Result> LogAndFailAsync(ProcessingWebhookLog log, string errorMessage)
        {
            log.ErrorMessage = errorMessage;
            _logger.LogWarning("{Prefix} {ErrorMessage}", Prefix, errorMessage);
            return await SaveLogAsync(log, "Error", Result.Failure(errorMessage));
        }

        #endregion Webhook Log
    }
}
