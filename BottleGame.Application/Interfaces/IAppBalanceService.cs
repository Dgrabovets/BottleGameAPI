using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Application balance
    /// </summary>
    public interface IAppBalanceService
    {
        #region App Balance Methods

        /// <summary>
        /// Gets the current application balance
        /// </summary>
        /// <returns>Balance information</returns>
        Task<Result<AppBalanceDTO>> GetAppBalance();

        /// <summary>
        /// Updates the lower threshold of the application balance
        /// </summary>
        /// <param name="request">Request with the new threshold</param>
        /// <param name="adminId">Administrator id</param>
        /// <returns>Operation result</returns>
        Task<Result> UpdateAppBalanceThreshold(UpdateAppBalanceThresholdRequest request, int adminId);

        /// <summary>
        /// Tops up the application balance
        /// </summary>
        /// <param name="request">Top-up request</param>
        /// <param name="adminId">Administrator id</param>
        /// <returns>Operation result with the new balance</returns>
        Task<Result<decimal>> TopUpAppBalance(TopUpAppBalanceRequest request, int adminId);

        #endregion App Balance Methods
    }
}
