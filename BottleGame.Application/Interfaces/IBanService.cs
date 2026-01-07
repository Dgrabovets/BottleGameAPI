using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса для работы с банами игроков
    /// </summary>
    public interface IBanService
    {
        #region Methods

        /// <summary>
        /// Проверить, забанен ли игрок
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="requestingPlayerId">ID запрашивающего (для проверки прав)</param>
        /// <param name="isAdmin">Является ли администратором</param>
        /// <returns>Информация о бане</returns>
        Task<Result<IsBannedResponse>> IsBanned(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Забанить игрока
        /// </summary>
        /// <param name="request">Запрос на бан</param>
        /// <returns>Результат операции</returns>
        Task<Result> BanPlayer(BanPlayerRequest request);

        /// <summary>
        /// Разбанить игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <returns>Результат операции</returns>
        Task<Result> UnbanPlayer(int playerId);

        #endregion Methods
    }
}



