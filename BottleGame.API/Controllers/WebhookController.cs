using BottleGame.Domain.Constants;
using BottleGame.Infrastructure.Data;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.External.Processing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер webhook-уведомлений от платёжной системы.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="WebhookController"/>.
    /// </remarks>
    /// <param name="context">Контекст базы данных.</param>
    /// <param name="logger">Логгер для записи событий.</param>
    /// <param name="options">Настройки платёжного процессинга.</param>
    [ApiController]
    [Route("api/webhook")]
    [Tags("Webhooks")]
    public class WebhookController(
        ApplicationDbContext context,
        ILogger<WebhookController> logger,
        IOptions<ProcessingOptions> options) : ControllerBase
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных.
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер для записи событий.
        /// </summary>
        private readonly ILogger<WebhookController> _logger = logger;

        /// <summary>
        /// Настройки платёжного процессинга.
        /// </summary>
        private readonly ProcessingOptions _options = options.Value;

        /// <summary>
        /// Префикс для логов.
        /// </summary>
        private const string Prefix = "[Webhook]";

        /// <summary>
        /// Настройки JSON-сериализации.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion Fields

        #region Action Methods

        /// <summary>
        /// Webhook для уведомлений об изменении статуса ордера (депозита).
        /// </summary>
        /// <returns>Результат обработки webhook.</returns>
        [HttpPost("order-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> OrderStatusWebhook()
        {
            var webhookLog = CreateWebhookLog("order-status");

            try
            {
                var body = await ReadRequestBodyAsync();
                webhookLog.RawRequest = body;

                _logger.LogInformation("{Prefix} Получен webhook order-status: {Body}", Prefix, body);

                var payload = JsonSerializer.Deserialize<OrderWebhookPayload>(body, JsonOptions);
                if (payload is null)
                {
                    return await LogAndReturnError(webhookLog, "Не удалось распарсить payload");
                }

                // Проверяем merchant_id
                if (payload.MerchantId != _options.MerchantId)
                {
                    return await LogAndReturnError(webhookLog, $"Неверный merchant_id: {payload.MerchantId}");
                }

                webhookLog.OrderId = payload.Uuid;
                webhookLog.TransactionId = payload.ExternalId;
                webhookLog.Result = $"{payload.Status}/{payload.SubStatus}";

                // Ищем транзакцию по external_id
                if (!int.TryParse(payload.ExternalId, out var transactionId))
                {
                    return await LogAndReturnError(webhookLog, $"Некорректный external_id: {payload.ExternalId}");
                }

                var transaction = await _context.MoneyTransactions
                    .Include(t => t.Player).ThenInclude(p => p.Balance)
                    .FirstOrDefaultAsync(t => t.Id == transactionId);

                if (transaction is null)
                {
                    return await LogAndReturnError(webhookLog, $"Транзакция не найдена: {transactionId}");
                }

                // Обрабатываем в зависимости от статуса
                IActionResult result = payload.Status switch
                {
                    OrderStatus.Success => await ProcessSuccessfulPayment(transaction, payload),
                    OrderStatus.Fail => await ProcessFailedPayment(transaction),
                    _ => Ok() // pending и другие статусы просто логируем
                };

                webhookLog.ProcessingStatus = "Success";
                webhookLog.ProcessedAt = DateTime.UtcNow;
                await SaveWebhookLogAsync(webhookLog);

                return result;
            }
            catch (Exception ex)
            {
                return await LogAndReturnException(webhookLog, ex);
            }
        }

        /// <summary>
        /// Webhook для уведомлений об изменении статуса выплаты.
        /// </summary>
        /// <returns>Результат обработки webhook.</returns>
        [HttpPost("payout-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PayoutStatusWebhook()
        {
            var webhookLog = CreateWebhookLog("payout-status");

            try
            {
                var body = await ReadRequestBodyAsync();
                webhookLog.RawRequest = body;

                _logger.LogInformation("{Prefix} Получен webhook payout-status: {Body}", Prefix, body);

                var payload = JsonSerializer.Deserialize<PayoutWebhookPayload>(body, JsonOptions);
                if (payload is null)
                {
                    return await LogAndReturnError(webhookLog, "Не удалось распарсить payload");
                }

                // Проверяем merchant_id
                if (payload.MerchantId != _options.MerchantId)
                {
                    return await LogAndReturnError(webhookLog, $"Неверный merchant_id: {payload.MerchantId}");
                }

                webhookLog.OrderId = payload.Uuid;
                webhookLog.TransactionId = payload.ExternalId;
                webhookLog.Result = $"{payload.Status}/{payload.SubStatus}";

                // Ищем транзакцию по external_id
                if (!int.TryParse(payload.ExternalId, out var transactionId))
                {
                    return await LogAndReturnError(webhookLog, $"Некорректный external_id: {payload.ExternalId}");
                }

                var transaction = await _context.MoneyTransactions
                    .Include(t => t.Player).ThenInclude(p => p.Balance)
                    .FirstOrDefaultAsync(t => t.Id == transactionId);

                if (transaction is null)
                {
                    return await LogAndReturnError(webhookLog, $"Транзакция не найдена: {transactionId}");
                }

                // Обрабатываем в зависимости от статуса
                IActionResult result = payload.Status switch
                {
                    PayoutStatus.Success => await ProcessSuccessfulPayout(transaction),
                    PayoutStatus.Fail => await ProcessFailedPayout(transaction, payload.SubStatus),
                    _ => Ok() // pending и другие статусы просто логируем
                };

                webhookLog.ProcessingStatus = "Success";
                webhookLog.ProcessedAt = DateTime.UtcNow;
                await SaveWebhookLogAsync(webhookLog);

                return result;
            }
            catch (Exception ex)
            {
                return await LogAndReturnException(webhookLog, ex);
            }
        }

        #endregion Action Methods

        #region Helper Methods

        /// <summary>
        /// Создаёт объект лога webhook'а.
        /// </summary>
        /// <param name="webhookType">Тип webhook.</param>
        /// <returns>Объект лога.</returns>
        private ProcessingWebhookLog CreateWebhookLog(string webhookType)
        {
            return new ProcessingWebhookLog
            {
                HttpMethod = Request.Method,
                ContentType = Request.ContentType,
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                ReceivedAt = DateTime.UtcNow,
                WebhookType = webhookType
            };
        }

        /// <summary>
        /// Читает тело HTTP-запроса.
        /// </summary>
        /// <returns>Строка с телом запроса.</returns>
        private async Task<string> ReadRequestBodyAsync()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
            return body;
        }

        /// <summary>
        /// Получает IP-адрес клиента.
        /// </summary>
        /// <returns>IP-адрес клиента.</returns>
        private string GetClientIpAddress()
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? Request.Headers["X-Real-IP"].FirstOrDefault()
                     ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            return ip ?? "Unknown";
        }

        /// <summary>
        /// Сохраняет лог webhook'а в базу данных.
        /// </summary>
        /// <param name="log">Объект лога.</param>
        private async Task SaveWebhookLogAsync(ProcessingWebhookLog log)
        {
            try
            {
                _context.ProcessingWebhookLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка сохранения лога webhook", Prefix);
            }
        }

        /// <summary>
        /// Логирует ошибку и возвращает BadRequest.
        /// </summary>
        /// <param name="webhookLog">Объект лога.</param>
        /// <param name="errorMessage">Сообщение об ошибке.</param>
        /// <returns>Результат с ошибкой 400.</returns>
        private async Task<IActionResult> LogAndReturnError(ProcessingWebhookLog webhookLog, string errorMessage)
        {
            webhookLog.ProcessingStatus = "Error";
            webhookLog.ErrorMessage = errorMessage;
            webhookLog.ProcessedAt = DateTime.UtcNow;
            await SaveWebhookLogAsync(webhookLog);

            _logger.LogWarning("{Prefix} {ErrorMessage}", Prefix, errorMessage);
            return BadRequest(errorMessage);
        }

        /// <summary>
        /// Логирует исключение и возвращает 500.
        /// </summary>
        /// <param name="webhookLog">Объект лога.</param>
        /// <param name="ex">Исключение.</param>
        /// <returns>Результат с ошибкой 500.</returns>
        private async Task<IActionResult> LogAndReturnException(ProcessingWebhookLog webhookLog, Exception ex)
        {
            webhookLog.ProcessingStatus = "Error";
            webhookLog.ErrorMessage = ex.ToString();
            webhookLog.ProcessedAt = DateTime.UtcNow;
            await SaveWebhookLogAsync(webhookLog);

            _logger.LogError(ex, "{Prefix} Ошибка обработки webhook", Prefix);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }

        #endregion Helper Methods

        #region Payment Processing

        /// <summary>
        /// Обрабатывает успешный платёж (депозит).
        /// </summary>
        /// <param name="transaction">Транзакция.</param>
        /// <param name="payload">Payload вебхука с данными ордера.</param>
        /// <returns>Результат обработки.</returns>
        private async Task<IActionResult> ProcessSuccessfulPayment(MoneyTransaction transaction, OrderWebhookPayload payload)
        {
            // Проверяем статистику сыгранных раундов
            var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == transaction.PlayerId);

            if (roundsPlayedStat is null)
            {
                _logger.LogWarning("{Prefix} Не найдена статистика сыгранных раундов для игрока {PlayerId}", Prefix, transaction.PlayerId);
                return BadRequest("Не найдена статистика сыгранных раундов игрока");
            }

            // Получаем баланс приложения
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Не найден баланс приложения", Prefix);
                return BadRequest("Не найден баланс приложения");
            }

            // Обновляем транзакцию с данными из процессинга
            transaction.IsProcessingSuccess = true;
            transaction.TransactionStatusId = TransactionStatuses.Approved;
            transaction.ProcessingOrderId = payload.Uuid;
            transaction.ExchangeRate = payload.ExchangeRate;
            transaction.Currency = payload.Currency;
            transaction.PaymentGateway = payload.PaymentGateway;

            // Начисляем деньги игроку и приложению
            transaction.Player.Balance.Amount += transaction.Amount;
            appBalance.Amount += transaction.Amount;

            // Сбрасываем счётчик раундов
            roundsPlayedStat.MoneyTransaction = transaction;
            roundsPlayedStat.DepositTransactionId = transaction.Id;
            roundsPlayedStat.PlayedRoundsCount = 0;

            // Реферальный бонус
            await ProcessReferralBonus(transaction);

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Транзакция {TransactionId} успешно обработана", Prefix, transaction.Id);
            return Ok();
        }

        /// <summary>
        /// Обрабатывает неуспешный платёж (депозит).
        /// </summary>
        /// <param name="transaction">Транзакция.</param>
        /// <returns>Результат обработки.</returns>
        private async Task<IActionResult> ProcessFailedPayment(MoneyTransaction transaction)
        {
            transaction.IsProcessingSuccess = false;
            transaction.TransactionStatusId = TransactionStatuses.Rejected;
            transaction.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Транзакция {TransactionId} завершена с ошибкой", Prefix, transaction.Id);
            return Ok();
        }

        /// <summary>
        /// Обрабатывает успешную выплату.
        /// </summary>
        /// <param name="transaction">Транзакция.</param>
        /// <returns>Результат обработки.</returns>
        private async Task<IActionResult> ProcessSuccessfulPayout(MoneyTransaction transaction)
        {
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Не найден баланс приложения", Prefix);
                return BadRequest("Не найден баланс приложения");
            }

            transaction.IsProcessingSuccess = true;
            transaction.TransactionStatusId = TransactionStatuses.Approved;
            appBalance.Amount -= transaction.Amount;

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Выплата {TransactionId} успешно обработана", Prefix, transaction.Id);
            return Ok();
        }

        /// <summary>
        /// Обрабатывает неуспешную выплату.
        /// </summary>
        /// <param name="transaction">Транзакция.</param>
        /// <param name="reason">Причина ошибки.</param>
        /// <returns>Результат обработки.</returns>
        private async Task<IActionResult> ProcessFailedPayout(MoneyTransaction transaction, string? reason)
        {
            transaction.IsProcessingSuccess = false;
            transaction.TransactionStatusId = TransactionStatuses.Rejected;

            // Возвращаем средства игроку
            transaction.Player.Balance.Amount += transaction.Amount;

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogWarning("{Prefix} Ошибка выплаты {TransactionId}: {Reason}", Prefix, transaction.Id, reason);
            return Ok();
        }

        /// <summary>
        /// Начисляет реферальный бонус.
        /// </summary>
        /// <param name="transaction">Транзакция депозита.</param>
        private async Task ProcessReferralBonus(MoneyTransaction transaction)
        {
            var invitation = await _context.InvitedPlayers
                .Include(ip => ip.PlayerInvited)
                .ThenInclude(p => p.Balance)
                .FirstOrDefaultAsync(ip => ip.PlayerIdInvited == transaction.PlayerId);

            if (invitation is not null && (invitation.PlayerInvited.Ban is null || !invitation.PlayerInvited.Ban.IsActive))
            {
                var referrer = invitation.PlayerInvited;
                var bonusAmount = (int)Math.Round(transaction.Amount * 0.1m, MidpointRounding.AwayFromZero);

                referrer.Balance.Amount += bonusAmount;

                // Обнуляем статистику сыгранных раундов для реферера
                var referrerRoundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                    .FirstOrDefaultAsync(s => s.PlayerId == referrer.Id);

                if (referrerRoundsPlayedStat is not null)
                {
                    referrerRoundsPlayedStat.MoneyTransaction = transaction;
                    referrerRoundsPlayedStat.DepositTransactionId = transaction.Id;
                    referrerRoundsPlayedStat.PlayedRoundsCount = 0;
                }

                _logger.LogInformation("{Prefix} Начислено {BonusAmount} рефереру {ReferrerId} за пополнение приглашённого игрока {PlayerId}",
                    Prefix, bonusAmount, referrer.Id, transaction.PlayerId);
            }
        }

        #endregion Payment Processing
    }
}
