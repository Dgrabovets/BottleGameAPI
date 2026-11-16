using BottleGame.Application.Common;
using BottleGame.Domain.Entities;

namespace BottleGame.Infrastructure.Services.Payments
{
    /// <summary>
    /// Handling of deposits and withdrawals. The implementation is chosen by the processing mode:
    /// Simulated completes operations locally, Live works through the payment provider
    /// </summary>
    public interface ITransactionProcessor
    {
        /// <summary>
        /// Checks the conditions under which a player can create a deposit or a withdrawal
        /// </summary>
        /// <param name="player">Player</param>
        /// <param name="isDeposit">true for a deposit, false for a withdrawal</param>
        /// <param name="gameParams">Game parameters</param>
        /// <returns>Success or the reason for the refusal</returns>
        Task<Result> CheckRequestAsync(Player player, bool isDeposit, GameParams gameParams);

        /// <summary>
        /// Starts a deposit for an already created transaction
        /// </summary>
        /// <param name="transaction">Deposit transaction</param>
        /// <param name="player">Player with loaded settings and balance</param>
        /// <returns>Data for the client response (payment link and order identifier) or an error</returns>
        Task<Result<object?>> StartDepositAsync(MoneyTransaction transaction, Player player);

        /// <summary>
        /// Approves a withdrawal: checks the conditions and sends the payout
        /// </summary>
        /// <param name="transaction">Withdrawal transaction with the player, their balance and settings</param>
        /// <param name="gameParams">Game parameters</param>
        /// <param name="appBalance">Application balance</param>
        /// <returns>Success or the reason for the refusal</returns>
        Task<Result> ApproveWithdrawalAsync(MoneyTransaction transaction, GameParams gameParams, AppBalance appBalance);
    }
}
