using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса для работы с игроками
    /// </summary>
    public interface IPlayerService
    {
        #region Ban Methods

        /// <summary>
        /// Проверить статус бана игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <returns>Информация о статусе бана</returns>
        Task<Result<IsBannedResponse>> IsBanned(int playerId);

        #endregion Ban Methods

        #region Get Methods

        /// <summary>
        /// Получить информацию об игроке по ID
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="requestingPlayerId">ID запрашивающего игрока (для проверки прав)</param>
        /// <param name="isAdmin">Является ли запрашивающий администратором</param>
        /// <returns>Информация об игроке</returns>
        Task<Result<PlayerInfoByIdResponse>> GetPlayerById(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Получить список всех игроков
        /// </summary>
        /// <returns>Список игроков</returns>
        Task<Result<IEnumerable<PlayerInfoResponse>>> GetAllPlayers();

        /// <summary>
        /// Получить топ-100 игроков по балансу
        /// </summary>
        /// <returns>Список топ-100 игроков</returns>
        Task<Result<IEnumerable<PlayerInfoResponse>>> GetTop100Players();

        /// <summary>
        /// Получить статистику по всем игрокам
        /// </summary>
        /// <returns>Статистика игроков</returns>
        Task<Result<IEnumerable<PlayerStatisticsResponse>>> GetAllPlayersStatistics();

        /// <summary>
        /// Получить статистику по конкретному игроку
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="requestingPlayerId">ID запрашивающего</param>
        /// <param name="isAdmin">Является ли администратором</param>
        /// <returns>Статистика игрока</returns>
        Task<Result<PlayerStatisticsResponse>> GetPlayerStatistics(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Получить статистику сыгранных раундов для вывода
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="requestingPlayerId">ID запрашивающего</param>
        /// <param name="isAdmin">Является ли администратором</param>
        /// <returns>Статистика раундов для вывода</returns>
        Task<Result<PlayerRoundsStatisticsForWithdrawalDTO>> GetPlayerRoundsStatisticsForWithdrawal(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Получить статистику сыгранных раундов для вывода по всем игрокам
        /// </summary>
        /// <returns>Список статистики</returns>
        Task<Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>> GetAllPlayersRoundsStatisticsForWithdrawal();

        #endregion Get Methods

        #region Update Methods

        /// <summary>
        /// Редактировать данные игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="request">Данные для изменения</param>
        /// <param name="isAdmin">Является ли запрашивающий администратором</param>
        /// <returns>Результат операции</returns>
        Task<Result> EditPlayer(int playerId, EditPlayerRequest request, bool isAdmin);

        #endregion Update Methods
    }
}



