using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Moderator management
    /// </summary>
    public interface IModeratorService
    {
        #region Moderator Methods

        /// <summary>
        /// Registers a new moderator
        /// </summary>
        /// <param name="request">Moderator data</param>
        /// <returns>Operation result</returns>
        Task<Result> RegisterModerator(AdministrationRoleUserCredentials request);

        /// <summary>
        /// Gets the list of moderators or a single moderator
        /// </summary>
        /// <param name="moderatorId">Moderator id (optional)</param>
        /// <returns>List or a single moderator</returns>
        Task<Result<object>> GetModerators(int? moderatorId);

        /// <summary>
        /// Deletes a moderator
        /// </summary>
        /// <param name="moderatorId">Moderator id</param>
        /// <returns>Operation result</returns>
        Task<Result> DeleteModerator(int moderatorId);

        #endregion Moderator Methods
    }
}
