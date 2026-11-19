using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Interface of the service for working with player bans
    /// </summary>
    public interface IBanService
    {
        #region Methods

        /// <summary>
        /// Checks whether a player is banned
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="requestingPlayerId">Id of the requesting player (for the access check)</param>
        /// <param name="isAdmin">Whether the requester is an administrator</param>
        /// <returns>Ban information</returns>
        Task<Result<IsBannedResponse>> IsBanned(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Bans a player
        /// </summary>
        /// <param name="request">Ban request</param>
        /// <returns>Operation result</returns>
        Task<Result> BanPlayer(BanPlayerRequest request);

        /// <summary>
        /// Unbans a player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <returns>Operation result</returns>
        Task<Result> UnbanPlayer(int playerId);

        #endregion Methods
    }
}
