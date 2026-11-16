using BottleGame.Application.Common;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services.Payments
{
    /// <summary>
    /// Режим Simulated: пополнения и выводы завершаются локально без платёжного провайдера и реальных денег
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="SimulatedTransactionProcessor"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    public class SimulatedTransactionProcessor(
        ApplicationDbContext context,
        ILogger<SimulatedTransactionProcessor> logger) : ITransactionProcessor
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<SimulatedTransactionProcessor> _logger = logger;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[SimulatedTransactionProcessor]";

        #endregion Fields

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
            CompleteWithdrawalLocally(transaction, appBalance, TransactionStatuses.Approved);
            _logger.LogInformation("{Prefix} Вывод {TransactionId} одобрен локально без процессинга", Prefix, transaction.Id);
            return Task.FromResult(Result.Success());
        }

        /// <summary>
        /// Локально завершает пополнение без обращения к процессинговой площадке
        /// </summary>
        /// <param name="transaction">Транзакция пополнения</param>
        /// <param name="player">Игрок</param>
        /// <param name="amount">Сумма пополнения</param>
        /// <returns>Ответ API с идентификатором ордера</returns>
        private async Task<object> CompleteDepositLocallyAsync(
            MoneyTransaction transaction,
            Player player,
            decimal amount)
        {
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Не найден баланс приложения");

            transaction.TransactionStatusId = TransactionStatuses.Approved;
            transaction.IsProcessingSuccess = true;
            transaction.ProcessedAt = DateTime.UtcNow;
            transaction.ProcessingOrderId = $"test-deposit-{transaction.Id}";
            transaction.PaymentGateway = player.PlayerOptions!.PaymentMethod.ToString();
            transaction.Currency = player.PlayerOptions.Currency?.ToString() ?? "rub";

            player.Balance!.Amount += amount;
            appBalance.Amount += amount;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "{Prefix} [Simulated] Пополнение {TransactionId} завершено локально без процессинга",
                Prefix,
                transaction.Id);

            return new
            {
                PaymentLink = (string?)null,
                OrderId = transaction.ProcessingOrderId
            };
        }

        /// <summary>
        /// Локально завершает вывод без обращения к процессинговой площадке
        /// </summary>
        /// <param name="transaction">Транзакция вывода</param>
        /// <param name="appBalance">Баланс приложения</param>
        /// <param name="approvedStatusId">Идентификатор статуса «Одобрено»</param>
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
