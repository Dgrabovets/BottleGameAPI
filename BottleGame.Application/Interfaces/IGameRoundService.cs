using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса для работы с игровыми раундами
    /// </summary>
    public interface IGameRoundService
    {
        #region Methods

        /// <summary>
        /// Завершить игровой раунд
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="request">Данные раунда</param>
        /// <param name="requestingPlayerId">ID запрашивающего (для проверки прав)</param>
        /// <param name="isAdmin">Является ли администратором</param>
        /// <returns>Результат операции</returns>
        Task<Result> FinishGameRound(int playerId, FinishRoundRequest request, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Получить сыгранные раунды игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <returns>Список раундов</returns>
        Task<Result<GetPlayerPlayedRoundsResponse>> GetPlayerPlayedRounds(int playerId);

        /// <summary>
        /// Получить прибыль за период
        /// </summary>
        /// <param name="request">Параметры периода</param>
        /// <returns>Информация о прибыли</returns>
        Task<Result<GetProfitResponse>> GetProfit(GetProfitRequest request);

        #endregion Methods
    }
}



