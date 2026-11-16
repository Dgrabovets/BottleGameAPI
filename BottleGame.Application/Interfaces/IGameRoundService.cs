using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Interface of the service for working with game rounds
    /// </summary>
    public interface IGameRoundService
    {
        #region Methods

        /// <summary>
        /// Finishes a game round
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="request">Round data</param>
        /// <param name="requestingPlayerId">Id of the requesting player (for the access check)</param>
        /// <param name="isAdmin">Whether the requester is an administrator</param>
        /// <returns>Operation result</returns>
        Task<Result> FinishGameRound(int playerId, FinishRoundRequest request, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Gets the rounds played by a player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <returns>List of rounds</returns>
        Task<Result<GetPlayerPlayedRoundsResponse>> GetPlayerPlayedRounds(int playerId);

        /// <summary>
        /// Gets the profit for a period
        /// </summary>
        /// <param name="request">Period parameters</param>
        /// <returns>Profit information</returns>
        Task<Result<GetProfitResponse>> GetProfit(GetProfitRequest request);

        #endregion Methods
    }
}



