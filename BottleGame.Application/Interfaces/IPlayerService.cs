using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Interface of the service for working with players
    /// </summary>
    public interface IPlayerService
    {
        #region Ban Methods

        /// <summary>
        /// Checks the ban status of a player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <returns>Ban status information</returns>
        Task<Result<IsBannedResponse>> IsBanned(int playerId);

        #endregion Ban Methods

        #region Get Methods

        /// <summary>
        /// Gets information about a player by id
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="requestingPlayerId">Id of the requesting player (for the access check)</param>
        /// <param name="isAdmin">Whether the requester is an administrator</param>
        /// <returns>Player information</returns>
        Task<Result<PlayerInfoByIdResponse>> GetPlayerById(int playerId, int? requestingPlayerId, bool isAdmin);

        /// <summary>
        /// Gets the list of players (without a search, actively banned players are left out)
        /// </summary>
        /// <param name="telegramId">Exact Telegram id (search, includes banned players)</param>
        /// <param name="name">Partial match on the nickname (search, includes banned players)</param>
        /// <returns>List of players</returns>
        Task<Result<IEnumerable<PlayerInfoResponse>>> GetAllPlayers(long? telegramId = null, string? name = null);

        /// <summary>
        /// Gets the top 100 players by balance
        /// </summary>
        /// <returns>List of the top 100 players</returns>
        Task<Result<IEnumerable<PlayerInfoResponse>>> GetTop100Players();

        #endregion Get Methods

        #region Update Methods

        /// <summary>
        /// Edits the data of a player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="request">Data to change</param>
        /// <param name="isAdmin">Whether the requester is an administrator</param>
        /// <returns>Operation result</returns>
        Task<Result> EditPlayer(int playerId, EditPlayerRequest request, bool isAdmin);

        #endregion Update Methods
    }
}



