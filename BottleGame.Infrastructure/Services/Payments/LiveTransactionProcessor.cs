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
    /// Режим Live: платежи и выплаты проходят через платёжного провайдера, итог приходит уведомлением
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="LiveTransactionProcessor"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    /// <param name="paymentProvider">Платёжный провайдер</param>
    public class LiveTransactionProcessor(
        ApplicationDbContext context,
        ILogger<LiveTransactionProcessor> logger,
        IPaymentProvider paymentProvider) : ITransactionProcessor
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<LiveTransactionProcessor> _logger = logger;

        /// <summary>
        /// Платёжный провайдер
        /// </summary>
        private readonly IPaymentProvider _paymentProvider = paymentProvider;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[LiveTransactionProcessor]";

        /// <summary>
        /// Максимальная доля нижнего порога баланса приложения, доступная для одной выплаты
        /// </summary>
        private const decimal MaxPayoutShareOfLowerThreshold = 0.1m;

        /// <summary>
        /// Валюта по умолчанию, если у игрока она не задана
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
                _logger.LogWarning("{Prefix} Не найдены данные статистики сыгранных раундов для игрока {PlayerId}", Prefix, player.Id);
                return Result.Failure("Не найдены данные статистики сыгранных раундов игрока");
            }

            if (!isDeposit && roundsPlayedStat.PlayedRoundsCount < gameParams.MinRoundsCountForWithdrawal)
            {
                _logger.LogWarning("{Prefix} Недостаточно сыгранных раундов для вывода у игрока {PlayerId}", Prefix, player.Id);
                return Result.Failure($"Для вывода необходимо сыграть минимум {gameParams.MinRoundsCountForWithdrawal} раундов. Сейчас сыграно: {roundsPlayedStat.PlayedRoundsCount}");
            }

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<object?>> StartDepositAsync(MoneyTransaction transaction, Player player)
        {
            try
            {
                _logger.LogInformation("{Prefix} Создание запроса для платежа игрока {PlayerId}, сумма {Amount}", Prefix, player.Id, transaction.Amount);

                var currency = player.PlayerOptions?.Currency ?? DefaultCurrency;
                var paymentRequest = new CreatePaymentRequest(
                    transaction.Id,
                    transaction.Amount,
                    currency,
                    $"Пополнение баланса, транзакция {transaction.Id}");

                var payment = await _paymentProvider.CreatePaymentAsync(paymentRequest);

                if (!payment.IsSuccess || string.IsNullOrEmpty(payment.PaymentUrl))
                {
                    await RejectFailedTransactionAsync(transaction, payment.Error);

                    _logger.LogError("{Prefix} Ошибка при создании платежа для игрока {PlayerId}: {ErrorMessage}", Prefix, player.Id, payment.Error);
                    return Result<object?>.InternalError("Ошибка при создании платежа: " + payment.Error);
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

                _logger.LogError(ex, "{Prefix} Ошибка при создании платежа для игрока {PlayerId}", Prefix, player.Id);
                return Result<object?>.InternalError("Ошибка при создании платежа");
            }
        }

        /// <inheritdoc/>
        public async Task<Result> ApproveWithdrawalAsync(MoneyTransaction transaction, GameParams gameParams, AppBalance appBalance)
        {
            var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == transaction.PlayerId);

            if (roundsPlayedStat is null)
            {
                return Result.Failure("Не найдены данные статистики сыгранных раундов игрока");
            }

            var minRoundsCount = gameParams.MinRoundsCountForWithdrawal;
            if (roundsPlayedStat.PlayedRoundsCount < minRoundsCount)
            {
                _logger.LogWarning("{Prefix} Недостаточно сыгранных раундов для вывода у игрока {PlayerId}", Prefix, transaction.PlayerId);
                return Result.Failure($"Для вывода необходимо сыграть минимум {minRoundsCount} раундов. Сейчас сыграно: {roundsPlayedStat.PlayedRoundsCount}");
            }

            if (transaction.Amount > appBalance.LowerThreshold * MaxPayoutShareOfLowerThreshold)
            {
                _logger.LogWarning("{Prefix} Одобрение транзакции невозможно - сумма превышает порог", Prefix);
                return Result.Failure("Одобрение транзакции невозможно - сумма к выводу превышает нижний порог баланса приложения!");
            }

            try
            {
                var playerOptions = transaction.Player.PlayerOptions!;
                var currency = playerOptions.Currency ?? DefaultCurrency;
                var payoutRequest = new CreatePayoutRequest(
                    transaction.Id,
                    transaction.Amount,
                    currency,
                    playerOptions.PayoutMethod,
                    playerOptions.Wallet ?? string.Empty,
                    $"Вывод средств, транзакция {transaction.Id}");

                var payout = await _paymentProvider.CreatePayoutAsync(payoutRequest);

                if (!payout.IsSuccess)
                {
                    await RefundFailedWithdrawalAsync(transaction, payout.Error);

                    _logger.LogError("{Prefix} Ошибка при создании выплаты для игрока {PlayerId}: {ErrorMessage}", Prefix, transaction.PlayerId, payout.Error);
                    return Result.InternalError("Ошибка при создании выплаты: " + payout.Error);
                }

                transaction.ProcessingOrderId = payout.PayoutId;
                transaction.PaymentGateway = playerOptions.PayoutMethod;
                transaction.Currency = currency;
                transaction.TransactionStatusId = TransactionStatuses.Approved;

                return Result.Success();
            }
            catch (Exception ex)
            {
                await RefundFailedWithdrawalAsync(transaction, null);

                _logger.LogError(ex, "{Prefix} Ошибка при создании выплаты для игрока {PlayerId}", Prefix, transaction.PlayerId);
                return Result.InternalError("Ошибка при создании выплаты");
            }
        }

        /// <summary>
        /// Отклоняет пополнение, которое не удалось создать у платёжного провайдера
        /// </summary>
        /// <param name="transaction">Транзакция пополнения</param>
        /// <param name="error">Текст ошибки процессинга</param>
        private async Task RejectFailedTransactionAsync(MoneyTransaction transaction, string? error)
        {
            transaction.TransactionStatusId = TransactionStatuses.Rejected;
            transaction.IsProcessingSuccess = false;
            transaction.ProcessingError = error;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Отклоняет вывод, который не удалось создать у платёжного провайдера, и возвращает средства игроку
        /// </summary>
        /// <param name="transaction">Транзакция вывода</param>
        /// <param name="error">Текст ошибки процессинга</param>
        private async Task RefundFailedWithdrawalAsync(MoneyTransaction transaction, string? error)
        {
            transaction.Player.Balance.Amount += transaction.Amount;
            transaction.ProcessedAt = DateTime.UtcNow;
            await RejectFailedTransactionAsync(transaction, error);
        }

        #endregion Processing
    }
}
