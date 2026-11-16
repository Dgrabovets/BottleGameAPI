using System.Text.Json;
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

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис обработки уведомлений ЮKassa о платежах и выплатах
    /// </summary>
    /// <remarks>
    /// Уведомления ЮKassa не подписываются, поэтому из тела берётся только идентификатор объекта.
    /// Статус, сумма и привязка к транзакции читаются из ответа API ЮKassa на запрос по этому идентификатору.
    /// Инициализирует новый экземпляр <see cref="PaymentWebhookService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="paymentProvider">Платёжный провайдер</param>
    /// <param name="logger">Логгер</param>
    public class PaymentWebhookService(
        ApplicationDbContext context,
        IPaymentProvider paymentProvider,
        ILogger<PaymentWebhookService> logger) : IPaymentWebhookService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Платёжный провайдер
        /// </summary>
        private readonly IPaymentProvider _paymentProvider = paymentProvider;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<PaymentWebhookService> _logger = logger;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[Webhook]";

        /// <summary>
        /// Доля реферального бонуса от суммы пополнения приглашённого игрока
        /// </summary>
        private const decimal ReferralBonusRate = 0.1m;

        /// <summary>
        /// Настройки JSON-десериализации
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        #endregion Fields

        #region Public Methods

        /// <inheritdoc/>
        public async Task<Result> HandleNotificationAsync(WebhookRequestInfo request)
        {
            var webhookLog = CreateWebhookLog(request);

            try
            {
                _logger.LogInformation("{Prefix} Получено уведомление: {Body}", Prefix, request.Body);

                var notification = TryParseNotification(request.Body);
                if (notification?.Object is null || string.IsNullOrWhiteSpace(notification.Object.Id))
                {
                    return await LogAndFailAsync(webhookLog, "Не удалось распарсить уведомление");
                }

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
                _logger.LogError(ex, "{Prefix} Ошибка обработки уведомления", Prefix);
                await SaveLogAsync(webhookLog, "Error", Result.InternalError());
                return Result.InternalError();
            }
        }

        #endregion Public Methods

        #region Webhook Pipeline

        /// <summary>
        /// Проверяет объект у провайдера, находит транзакцию и применяет итог один раз
        /// </summary>
        /// <param name="webhookLog">Запись журнала</param>
        /// <param name="providerId">Идентификатор платежа или выплаты у провайдера</param>
        /// <param name="isPayout">true для выплаты, false для платежа</param>
        /// <returns>Результат обработки</returns>
        private async Task<Result> HandleAsync(ProcessingWebhookLog webhookLog, string providerId, bool isPayout)
        {
            var operation = isPayout
                ? await _paymentProvider.GetPayoutAsync(providerId)
                : await _paymentProvider.GetPaymentAsync(providerId);

            if (operation is null)
            {
                return await LogAndFailAsync(webhookLog, $"Объект не найден у провайдера: {providerId}");
            }

            // Объект получен напрямую от провайдера, значит уведомление не подделано.
            webhookLog.IsSignatureValid = true;
            webhookLog.Result = operation.Status.ToString();

            if (operation.TransactionId is not { } transactionId)
            {
                return await LogAndFailAsync(webhookLog, $"У объекта {providerId} нет идентификатора транзакции");
            }

            webhookLog.TransactionId = transactionId.ToString();

            var transaction = await _context.MoneyTransactions
                .Include(t => t.Player).ThenInclude(p => p.Balance)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction is null)
            {
                return await LogAndFailAsync(webhookLog, $"Транзакция не найдена: {transactionId}");
            }

            var expectedType = isPayout ? TransactionTypes.Withdrawal : TransactionTypes.Deposit;
            if (transaction.TransactionTypeId != expectedType)
            {
                return await LogAndFailAsync(webhookLog, $"Тип транзакции {transactionId} не соответствует событию");
            }

            // Провайдер повторяет доставку; итог по транзакции фиксируется один раз,
            // иначе деньги были бы начислены или возвращены дважды.
            if (transaction.IsProcessingSuccess is not null)
            {
                _logger.LogWarning("{Prefix} Повторное уведомление по транзакции {TransactionId} проигнорировано", Prefix, transaction.Id);
                return await SaveLogAsync(webhookLog, "Duplicate", Result.Success());
            }

            if (operation.Amount != transaction.Amount)
            {
                return await LogAndFailAsync(webhookLog, $"Сумма у провайдера ({operation.Amount}) не совпадает с суммой транзакции ({transaction.Amount})");
            }

            Result result;
            try
            {
                result = (isPayout, operation.Status) switch
                {
                    (false, ProviderOperationStatus.Succeeded) => await ProcessSuccessfulPaymentAsync(transaction, operation),
                    (false, ProviderOperationStatus.Canceled) => await ProcessFailedPaymentAsync(transaction),
                    (true, ProviderOperationStatus.Succeeded) => await ProcessSuccessfulPayoutAsync(transaction),
                    (true, ProviderOperationStatus.Canceled) => await ProcessFailedPayoutAsync(transaction, operation.CancellationReason),
                    _ => Result.Success() // операция ещё не завершена
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                // Параллельный обработчик уже изменил транзакцию: его итог остаётся, этот запуск ничего не применяет.
                _context.ChangeTracker.Clear();
                _logger.LogWarning("{Prefix} Транзакция {TransactionId} обработана параллельным запросом, повтор проигнорирован", Prefix, transactionId);
                return await SaveLogAsync(webhookLog, "Duplicate", Result.Success());
            }

            return result.IsSuccess
                ? await SaveLogAsync(webhookLog, "Success", result)
                : await LogAndFailAsync(webhookLog, result.Error ?? "Ошибка обработки");
        }

        /// <summary>
        /// Разбирает тело уведомления
        /// </summary>
        /// <param name="body">Сырое тело запроса</param>
        /// <returns>Уведомление или null, если тело не является корректным JSON</returns>
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
        /// Обрабатывает успешный платёж (пополнение)
        /// </summary>
        /// <param name="transaction">Транзакция</param>
        /// <param name="payment">Платёж у провайдера</param>
        /// <returns>Результат обработки</returns>
        private async Task<Result> ProcessSuccessfulPaymentAsync(MoneyTransaction transaction, ProviderOperation payment)
        {
            var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == transaction.PlayerId);

            if (roundsPlayedStat is null)
            {
                _logger.LogWarning("{Prefix} Не найдена статистика сыгранных раундов для игрока {PlayerId}", Prefix, transaction.PlayerId);
                return Result.Failure("Не найдена статистика сыгранных раундов игрока");
            }

            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Не найден баланс приложения", Prefix);
                return Result.Failure("Не найден баланс приложения");
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

            _logger.LogInformation("{Prefix} Транзакция {TransactionId} успешно обработана", Prefix, transaction.Id);
            return Result.Success();
        }

        /// <summary>
        /// Обрабатывает неуспешный платёж (пополнение)
        /// </summary>
        /// <param name="transaction">Транзакция</param>
        /// <returns>Результат обработки</returns>
        private async Task<Result> ProcessFailedPaymentAsync(MoneyTransaction transaction)
        {
            transaction.IsProcessingSuccess = false;
            transaction.TransactionStatusId = TransactionStatuses.Rejected;
            transaction.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Транзакция {TransactionId} завершена с ошибкой", Prefix, transaction.Id);
            return Result.Success();
        }

        /// <summary>
        /// Обрабатывает успешную выплату
        /// </summary>
        /// <param name="transaction">Транзакция</param>
        /// <returns>Результат обработки</returns>
        private async Task<Result> ProcessSuccessfulPayoutAsync(MoneyTransaction transaction)
        {
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Не найден баланс приложения", Prefix);
                return Result.Failure("Не найден баланс приложения");
            }

            transaction.IsProcessingSuccess = true;
            transaction.TransactionStatusId = TransactionStatuses.Approved;
            appBalance.Amount -= transaction.Amount;

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Выплата {TransactionId} успешно обработана", Prefix, transaction.Id);
            return Result.Success();
        }

        /// <summary>
        /// Обрабатывает отменённую выплату и возвращает средства игроку
        /// </summary>
        /// <param name="transaction">Транзакция</param>
        /// <param name="reason">Причина отмены</param>
        /// <returns>Результат обработки</returns>
        private async Task<Result> ProcessFailedPayoutAsync(MoneyTransaction transaction, string? reason)
        {
            transaction.IsProcessingSuccess = false;
            transaction.TransactionStatusId = TransactionStatuses.Rejected;
            transaction.ProcessingError = reason;
            transaction.Player.Balance.Amount += transaction.Amount;

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogWarning("{Prefix} Выплата {TransactionId} отменена: {Reason}", Prefix, transaction.Id, reason);
            return Result.Success();
        }

        /// <summary>
        /// Начисляет реферальный бонус пригласившему игроку, если он не забанен
        /// </summary>
        /// <param name="transaction">Транзакция пополнения приглашённого игрока</param>
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
                "{Prefix} Начислено {BonusAmount} рефереру {ReferrerId} за пополнение приглашённого игрока {PlayerId}",
                Prefix, bonusAmount, referrer.Id, transaction.PlayerId);
        }

        /// <summary>
        /// Обнуляет счётчик сыгранных раундов после пополнения
        /// </summary>
        /// <param name="statistic">Статистика раундов игрока</param>
        /// <param name="depositTransaction">Транзакция пополнения</param>
        private static void ResetPlayedRounds(PlayerPlayedRoundsStatisticForWithdrawal statistic, MoneyTransaction depositTransaction)
        {
            statistic.MoneyTransaction = depositTransaction;
            statistic.DepositTransactionId = depositTransaction.Id;
            statistic.PlayedRoundsCount = 0;
        }

        #endregion Payment Processing

        #region Webhook Log

        /// <summary>
        /// Создаёт запись журнала по входящему запросу
        /// </summary>
        /// <param name="request">Входящий webhook-запрос</param>
        /// <returns>Запись журнала</returns>
        private static ProcessingWebhookLog CreateWebhookLog(WebhookRequestInfo request) => new()
        {
            RawRequest = request.Body,
            HttpMethod = request.HttpMethod,
            ContentType = request.ContentType,
            IpAddress = request.IpAddress ?? "Unknown",
            UserAgent = request.UserAgent,
            ReceivedAt = DateTime.UtcNow,
            WebhookType = "yookassa"
        };

        /// <summary>
        /// Сохраняет запись журнала с итоговым статусом
        /// </summary>
        /// <param name="log">Запись журнала</param>
        /// <param name="processingStatus">Итоговый статус обработки</param>
        /// <param name="result">Результат, который возвращается вызывающему</param>
        /// <returns>Переданный результат</returns>
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
                _logger.LogError(ex, "{Prefix} Ошибка сохранения лога webhook", Prefix);
            }

            return result;
        }

        /// <summary>
        /// Записывает ошибку в журнал и возвращает неуспешный результат
        /// </summary>
        /// <param name="log">Запись журнала</param>
        /// <param name="errorMessage">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 400</returns>
        private async Task<Result> LogAndFailAsync(ProcessingWebhookLog log, string errorMessage)
        {
            log.ErrorMessage = errorMessage;
            _logger.LogWarning("{Prefix} {ErrorMessage}", Prefix, errorMessage);
            return await SaveLogAsync(log, "Error", Result.Failure(errorMessage));
        }

        #endregion Webhook Log
    }
}
