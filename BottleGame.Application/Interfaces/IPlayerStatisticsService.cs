using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Player statistics
    /// </summary>
    public interface IPlayerStatisticsService
    {
        /// <summary>
        /// Gets the statistics of all players
        /// </summary>
        /// <returns>Player statistics</returns>
        Task<Result<IEnumerable<PlayerStatisticsResponse>>> GetAllPlayersStatistics();

        /// <summary>
        /// Gets the statistics of a single player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="requestingPlayerId">Id of the requester</param>
        /// <param name="isAdmin">Whether the requester is an administrator</param>
        /// <returns>Player statistics</returns>
        Task<Result<PlayerStatisticsResponse>> GetPlayerStatistics(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Gets the statistics of the rounds played for a withdrawal
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="requestingPlayerId">Id of the requester</param>
        /// <param name="isAdmin">Whether the requester is an administrator</param>
        /// <returns>Statistics of the rounds for a withdrawal</returns>
        Task<Result<PlayerRoundsStatisticsForWithdrawalDTO>> GetPlayerRoundsStatisticsForWithdrawal(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Gets the statistics of the rounds played for a withdrawal by all players
        /// </summary>
        /// <returns>List of statistics</returns>
        Task<Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>> GetAllPlayersRoundsStatisticsForWithdrawal();
    }
}
