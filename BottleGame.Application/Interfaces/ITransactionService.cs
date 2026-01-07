using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса для работы с транзакциями
    /// </summary>
    public interface ITransactionService
    {
        #region Balance Methods

        /// <summary>
        /// Получить баланс игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="requestingPlayerId">ID запрашивающего</param>
        /// <param name="isAdmin">Является ли администратором</param>
        /// <returns>Информация о балансе</returns>
        Task<Result<GetPlayerBalanceResponse>> GetPlayerBalance(int playerId, int? requestingPlayerId, bool isAdmin);

        #endregion Balance Methods

        #region Transaction Methods

        /// <summary>
        /// Создать транзакцию (депозит или вывод)
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="request">Данные транзакции</param>
        /// <param name="requestingPlayerId">ID запрашивающего</param>
        /// <param name="isAdmin">Является ли администратором</param>
        /// <returns>Результат создания транзакции</returns>
        Task<Result<object?>> CreateTransaction(int playerId, MoneyTransactionRequest request, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Обновить статус транзакции
        /// </summary>
        /// <param name="transactionId">ID транзакции</param>
        /// <param name="request">Новый статус</param>
        /// <returns>Результат операции</returns>
        Task<Result> UpdateTransactionStatus(int transactionId, ChangeTransactionStatusRequest request);

        /// <summary>
        /// Получить транзакции игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="requestingPlayerId">ID запрашивающего</param>
        /// <param name="isAdmin">Является ли администратором</param>
        /// <returns>Список транзакций</returns>
        Task<Result<GetPlayerTransactionsResponse>> GetPlayerTransactions(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Получить все транзакции
        /// </summary>
        /// <param name="typeId">Фильтр по типу (опционально)</param>
        /// <returns>Список транзакций по игрокам</returns>
        Task<Result<IEnumerable<GetPlayerTransactionsResponse>>> GetAllTransactions(int? typeId);

        #endregion Transaction Methods

        #region Reference Data Methods

        /// <summary>
        /// Получить все статусы транзакций
        /// </summary>
        /// <returns>Список статусов</returns>
        Task<Result<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionStatuses();

        /// <summary>
        /// Получить все типы транзакций
        /// </summary>
        /// <returns>Список типов</returns>
        Task<Result<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionTypes();

        #endregion Reference Data Methods
    }
}



