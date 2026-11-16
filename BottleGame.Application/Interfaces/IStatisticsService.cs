using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Application statistics
    /// </summary>
    public interface IStatisticsService
    {
        #region Statistics Methods

        /// <summary>
        /// Gets the statistics for the administrator
        /// </summary>
        /// <param name="request">Optional period</param>
        /// <returns>Administration statistics</returns>
        Task<Result<GetAppGeneralStatisticsResponse>> GetStatistics(GetAppGeneralStatisticsRequest? request = null);

        /// <summary>
        /// Gets the statistics timeline for charts
        /// </summary>
        /// <param name="request">Period</param>
        /// <returns>Points of the timeline</returns>
        Task<Result<GetAppStatisticsTimelineResponse>> GetStatisticsTimeline(GetAppStatisticsTimelineRequest request);

        #endregion Statistics Methods
    }
}
