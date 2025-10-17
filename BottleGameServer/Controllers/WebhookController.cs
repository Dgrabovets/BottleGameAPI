using BottleGameServer.Data;
using BottleGameServer.Models.Entities;
using BottleGameServer.Processing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace BottleGameServer.Controllers
{
    [ApiController]
    [Route("api/webhook")]
    public class WebhookController : ControllerBase
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<WebhookController> _logger;

        /// <inheritdoc cref="ProcessingOptions"/>
        private readonly ProcessingOptions _options;

        const string prefix = "[Webhook]";

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Конструктор контроллера webhook
        /// </summary>
        /// <param name="context">Контекст базы данных</param>
        /// <param name="logger">Логгер</param>
        /// <param name="options">Опции конфигурации для взаимодействия с процессинговой площадкой</param>
        public WebhookController(ApplicationDbContext context, ILogger<WebhookController> logger, IOptions<ProcessingOptions> options)
        {
            _context = context;
            _logger = logger;
            _options = options.Value;
        }

        #endregion Constructor

        #region ActionMethods

        /// <summary>
        /// Универсальный webhook для уведомлений от NicePay (платежи и выплаты)
        /// Поддерживает как GET (query parameters), так и POST (JSON body)
        /// </summary>
        [HttpPost("notify")]
        [HttpGet("notify")] // Добавляем поддержку GET для обратной совместимости
        public async Task<IActionResult> UnifiedWebhook()
        {
            var webhookLog = new NicePayWebhookLog
            {
                HttpMethod = Request.Method,
                ContentType = Request.ContentType,
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                ReceivedAt = DateTime.UtcNow
            };

            try
            {
                // Получаем параметры из query string (для GET) или body (для POST)
                var parameters = await GetRequestParametersAsync();

                // Сохраняем raw запрос
                webhookLog.RawRequest = await GetRawRequestAsync();

                _logger.LogInformation("{Prefix} Получено уведомление с параметрами: {@Parameters}", prefix, parameters);

                // Сохраняем основные поля для удобства поиска
                if (parameters.ContainsKey("order_id"))
                    webhookLog.OrderId = parameters["order_id"];
                if (parameters.ContainsKey("result"))
                    webhookLog.Result = parameters["result"];

                // Проверяем наличие обязательных параметров
                if (!parameters.ContainsKey("result") || !parameters.ContainsKey("order_id"))
                {
                    webhookLog.ProcessingStatus = "Error";
                    webhookLog.ErrorMessage = "Отсутствуют обязательные параметры";
                    webhookLog.ProcessedAt = DateTime.UtcNow;
                    await SaveWebhookLogAsync(webhookLog);

                    _logger.LogWarning("{Prefix} Отсутствуют обязательные параметры", prefix);
                    return BadRequest("Отсутствуют обязательные параметры");
                }

                // Проверка merchant_id
                if (parameters.ContainsKey("merchant_id") && parameters["merchant_id"] != _options.MerchantId)
                {
                    webhookLog.ProcessingStatus = "Error";
                    webhookLog.ErrorMessage = "Неверный merchant_id";
                    webhookLog.ProcessedAt = DateTime.UtcNow;
                    await SaveWebhookLogAsync(webhookLog);

                    _logger.LogWarning("{Prefix} Неверный merchant_id: {MerchantId}", prefix, parameters["merchant_id"]);
                    return BadRequest("Неверный merchant_id");
                }

                // Проверка цифровой подписи
                var isSignatureValid = VerifyNicePayHash(parameters, _options.SecretKey);
                webhookLog.IsSignatureValid = isSignatureValid;

                if (!isSignatureValid)
                {
                    webhookLog.ProcessingStatus = "InvalidSignature";
                    webhookLog.ErrorMessage = "Некорректная цифровая подпись";
                    webhookLog.ProcessedAt = DateTime.UtcNow;
                    await SaveWebhookLogAsync(webhookLog);

                    _logger.LogWarning("{Prefix} Некорректная цифровая подпись webhook", prefix);
                    return BadRequest("Некорректная цифровая подпись");
                }

                // Преобразуем параметры в модель
                var request = MapToNicePayWebhookRequest(parameters);
                webhookLog.WebhookType = request.Type.ToString();

                // Валидация order_id
                if (!int.TryParse(request.order_id, out var transactionId))
                {
                    webhookLog.ProcessingStatus = "Error";
                    webhookLog.ErrorMessage = "Некорректный order_id";
                    webhookLog.ProcessedAt = DateTime.UtcNow;
                    await SaveWebhookLogAsync(webhookLog);

                    _logger.LogWarning("{Prefix} Некорректный order_id: {OrderId}", prefix, request.order_id);
                    return BadRequest("Некорректный order_id");
                }

                webhookLog.TransactionId = transactionId;

                // Обработка в зависимости от типа webhook
                IActionResult result;
                switch (request.Type)
                {
                    case WebhookType.Payment:
                        result = await ProcessPaymentWebhook(request, transactionId);
                        break;
                    case WebhookType.Payout:
                        result = await ProcessPayoutWebhook(request, transactionId);
                        break;
                    default:
                        result = BadRequest("Неизвестный тип webhook");
                        break;
                }

                webhookLog.ProcessingStatus = "Success";
                webhookLog.ProcessedAt = DateTime.UtcNow;
                await SaveWebhookLogAsync(webhookLog);

                return result;
            }
            catch (Exception ex)
            {
                webhookLog.ProcessingStatus = "Error";
                webhookLog.ErrorMessage = ex.Message;
                webhookLog.ProcessedAt = DateTime.UtcNow;
                await SaveWebhookLogAsync(webhookLog);

                _logger.LogError(ex, "{Prefix} Ошибка обработки webhook", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        #endregion ActionMethods

        #region Methods

        /// <summary>
        /// Получает параметры запроса из query string или body
        /// </summary>
        private async Task<Dictionary<string, string>> GetRequestParametersAsync()
        {
            var parameters = new Dictionary<string, string>();

            // Пробуем получить из query string (GET запрос)
            if (Request.Query.Any())
            {
                foreach (var (key, value) in Request.Query)
                {
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        parameters[key] = value.ToString();
                    }
                }
                return parameters;
            }

            // Пробуем получить из body (POST запрос с JSON)
            if (Request.ContentLength > 0)
            {
                Request.EnableBuffering();
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                Request.Body.Position = 0;

                if (!string.IsNullOrEmpty(body))
                {
                    try
                    {
                        var jsonParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
                        if (jsonParams != null)
                        {
                            foreach (var (key, value) in jsonParams)
                            {
                                parameters[key] = value?.ToString() ?? string.Empty;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "{Prefix} Ошибка десериализации JSON тела", prefix);
                    }
                }
            }

            return parameters;
        }

        /// <summary>
        /// Получает raw запрос
        /// </summary>
        private async Task<string> GetRawRequestAsync()
        {
            if (Request.Method == "GET")
            {
                return Request.QueryString.ToString();
            }
            else
            {
                Request.EnableBuffering();
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                Request.Body.Position = 0;
                return body;
            }
        }

        /// <summary>
        /// Получает IP адрес клиента
        /// </summary>
        private string GetClientIpAddress()
        {
            // Пробуем получить IP из различных заголовков
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? Request.Headers["X-Real-IP"].FirstOrDefault()
                     ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            return ip ?? "Unknown";
        }

        /// <summary>
        /// Сохраняет лог webhook
        /// </summary>
        private async Task SaveWebhookLogAsync(NicePayWebhookLog log)
        {
            try
            {
                _context.NicePayWebhookLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка сохранения лога webhook", prefix);
                // Не прерываем выполнение из-за ошибки логирования
            }
        }

        /// <summary>
        /// Преобразует словарь параметров в модель NicePayWebhookRequest
        /// </summary>
        private NicePayWebhookRequest MapToNicePayWebhookRequest(Dictionary<string, string> parameters)
        {
            var request = new NicePayWebhookRequest();

            var properties = typeof(NicePayWebhookRequest).GetProperties();
            foreach (var property in properties)
            {
                if (parameters.ContainsKey(property.Name))
                {
                    var value = parameters[property.Name];
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Для всех числовых типов
                        if ((property.PropertyType == typeof(int?) || property.PropertyType == typeof(int))
                            && int.TryParse(value, out var intValue))
                        {
                            property.SetValue(request, intValue);
                        }
                        else if (property.PropertyType == typeof(string))
                        {
                            property.SetValue(request, value);
                        }
                    }
                }
            }

            return request;
        }

        /// <summary>
        /// Проверка цифровой подписи в соответствии с PHP примером от NicePay
        /// </summary>
        private bool VerifyNicePayHash(Dictionary<string, string> parameters, string secretKey)
        {
            if (!parameters.ContainsKey("hash"))
            {
                _logger.LogWarning("{Prefix} Отсутствует параметр hash", prefix);
                return false;
            }

            var receivedHash = parameters["hash"];

            // Создаем копию параметров без hash
            var paramsForHash = new Dictionary<string, string>(parameters);
            paramsForHash.Remove("hash");

            // Сортируем по ключам в алфавитном порядке
            var sortedParams = paramsForHash
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .ToDictionary(p => p.Key, p => p.Value);

            // Получаем значения в отсортированном порядке
            var values = sortedParams.Values.ToList();

            // Добавляем секретный ключ в конец
            values.Add(secretKey);

            // Формируем строку с разделителем {np}
            var hashString = string.Join("{np}", values);

            // Вычисляем SHA256 хеш
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashString));
            var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            _logger.LogDebug("{Prefix} Вычисленный hash: {ComputedHash}, полученный hash: {ReceivedHash}",
                prefix, computedHash, receivedHash);

            return computedHash == receivedHash.ToLowerInvariant();
        }

        /// <summary>
        /// Обработка webhook платежа
        /// </summary>
        private async Task<IActionResult> ProcessPaymentWebhook(NicePayWebhookRequest request, int transactionId)
        {
            var transaction = await _context.MoneyTransactions
                .Include(t => t.Player)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction is null)
            {
                _logger.LogWarning("{Prefix} Транзакция не найдена для order_id: {OrderId}", prefix, request.order_id);
                return NotFound();
            }

            switch (request.result)
            {
                case "success":
                    return await ProcessSuccessfulPayment(transaction, request);
                case "error":
                    return await ProcessFailedPayment(transaction);
                default:
                    _logger.LogWarning("{Prefix} Неизвестный статус платежа: {Status}", prefix, request.result);
                    return BadRequest("Неизвестный статус");
            }
        }

        /// <summary>
        /// Обработка успешного платежа
        /// </summary>
        private async Task<IActionResult> ProcessSuccessfulPayment(MoneyTransaction transaction, NicePayWebhookRequest request)
        {
            // Получаем статистику сыгранных раундов для вывода
            var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == transaction.PlayerId);

            if (roundsPlayedStat is null)
            {
                _logger.LogWarning("{Prefix} Не найдены данные статистики сыгранных раундов для игрока {PlayerId}", prefix, transaction.PlayerId);
                return BadRequest("Не найдены данные статистики сыгранных раундов игрока");
            }

            // Получаем баланс приложения
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Не найдены данные баланса приложения", prefix);
                return BadRequest("Не найдены данные баланса приложения");
            }

            // Обновление транзакции
            transaction.IsProcessingSuccess = true;
            transaction.TransactionStatusId = 2;

            // Начисление средств игроку и приложению
            transaction.Player.Balance.Amount += transaction.Amount;
            appBalance.Amount += transaction.Amount;

            // Обновление статистики
            roundsPlayedStat.MoneyTransaction = transaction;
            roundsPlayedStat.DepositTransactionId = transaction.Id;
            roundsPlayedStat.PlayedRoundsCount = 0;

            // Реферальная система
            await ProcessReferralBonus(transaction);

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Транзакция {TransactionId} успешно обработана", prefix, transaction.Id);
            return Ok();
        }

        /// <summary>
        /// Обработка неуспешного платежа
        /// </summary>
        private async Task<IActionResult> ProcessFailedPayment(MoneyTransaction transaction)
        {
            transaction.IsProcessingSuccess = false;
            transaction.TransactionStatusId = 3;
            transaction.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Транзакция {TransactionId} завершена с ошибкой", prefix, transaction.Id);
            return Ok();
        }

        /// <summary>
        /// Обработка реферального бонуса
        /// </summary>
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

                // Получаем статистику сыгранных раундов для реферера
                var referrerRoundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                    .FirstOrDefaultAsync(s => s.PlayerId == referrer.Id);

                if (referrerRoundsPlayedStat is not null)
                {
                    referrerRoundsPlayedStat.MoneyTransaction = transaction;
                    referrerRoundsPlayedStat.DepositTransactionId = transaction.Id;
                    referrerRoundsPlayedStat.PlayedRoundsCount = 0;
                }

                _logger.LogInformation("{Prefix} Начислено {BonusAmount} игроку {ReferrerId} за пополнение приглашенным игроком {PlayerId}",
                    prefix, bonusAmount, referrer.Id, transaction.PlayerId);
            }
        }

        /// <summary>
        /// Обработка webhook выплаты
        /// </summary>
        private async Task<IActionResult> ProcessPayoutWebhook(NicePayWebhookRequest request, int transactionId)
        {
            var transaction = await _context.MoneyTransactions
                .Include(t => t.Player)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction is null)
            {
                _logger.LogWarning("{Prefix} Транзакция не найдена для order_id: {OrderId}", prefix, request.order_id);
                return NotFound();
            }

            if (request.result == "success_payout")
            {
                return await ProcessSuccessfulPayout(transaction);
            }
            else if (request.result == "error_payout")
            {
                return await ProcessFailedPayout(transaction, request.reason);
            }

            _logger.LogWarning("{Prefix} Неизвестный статус выплаты: {Status}", prefix, request.result);
            return BadRequest("Неизвестный статус выплаты");
        }

        /// <summary>
        /// Обработка успешной выплаты
        /// </summary>
        private async Task<IActionResult> ProcessSuccessfulPayout(MoneyTransaction transaction)
        {
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Не найдены данные баланса приложения", prefix);
                return BadRequest("Не найдены данные баланса приложения");
            }

            transaction.IsProcessingSuccess = true;
            transaction.TransactionStatusId = 2;
            appBalance.Amount -= transaction.Amount;

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Выплата {TransactionId} успешно обработана", prefix, transaction.Id);
            return Ok();
        }

        /// <summary>
        /// Обработка неуспешной выплаты
        /// </summary>
        private async Task<IActionResult> ProcessFailedPayout(MoneyTransaction transaction, string? reason)
        {
            transaction.IsProcessingSuccess = false;
            transaction.TransactionStatusId = 3;

            // Возвращаем средства игроку
            transaction.Player.Balance.Amount += transaction.Amount;

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogWarning("{Prefix} Ошибка выплаты {TransactionId}: {Reason}", prefix, transaction.Id, reason);
            return Ok();
        }

        #endregion Methods
    }
}