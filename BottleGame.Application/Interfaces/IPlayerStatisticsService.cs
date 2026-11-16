using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Статистика игроков
    /// </summary>
    public interface IPlayerStatisticsService
    {
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
    }
}
