using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Interface of the service for working with transactions
    /// </summary>
    public interface ITransactionService
    {
        #region Balance Methods

        /// <summary>
        /// Gets the balance of a player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="requestingPlayerId">Id of the requester</param>
        /// <param name="isAdmin">Whether the requester is an administrator</param>
        /// <returns>Balance information</returns>
        Task<Result<GetPlayerBalanceResponse>> GetPlayerBalance(int playerId, int? requestingPlayerId, bool isAdmin);

        #endregion Balance Methods

        #region Transaction Methods

        /// <summary>
        /// Creates a transaction (deposit or withdrawal)
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="request">Transaction data</param>
        /// <param name="requestingPlayerId">Id of the requester</param>
        /// <param name="isAdmin">Whether the requester is an administrator</param>
        /// <returns>Result of creating the transaction</returns>
        Task<Result<object?>> CreateTransaction(int playerId, MoneyTransactionRequest request, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Updates the status of a transaction
        /// </summary>
        /// <param name="transactionId">Transaction id</param>
        /// <param name="request">New status</param>
        /// <returns>Operation result</returns>
        Task<Result> UpdateTransactionStatus(int transactionId, ChangeTransactionStatusRequest request);

        /// <summary>
        /// Gets the transactions of a player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="requestingPlayerId">Id of the requester</param>
        /// <param name="isAdmin">Whether the requester is an administrator</param>
        /// <returns>List of transactions</returns>
        Task<Result<GetPlayerTransactionsResponse>> GetPlayerTransactions(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Gets all transactions
        /// </summary>
        /// <param name="typeId">Filter by type (optional)</param>
        /// <returns>List of transactions by player</returns>
        Task<Result<IEnumerable<GetPlayerTransactionsResponse>>> GetAllTransactions(int? typeId);

        #endregion Transaction Methods

        #region Reference Data Methods

        /// <summary>
        /// Gets all transaction statuses
        /// </summary>
        /// <returns>List of statuses</returns>
        Task<Result<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionStatuses();

        /// <summary>
        /// Gets all transaction types
        /// </summary>
        /// <returns>List of types</returns>
        Task<Result<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionTypes();

        #endregion Reference Data Methods
    }
}



