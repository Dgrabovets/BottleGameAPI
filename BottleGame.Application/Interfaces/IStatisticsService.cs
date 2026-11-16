using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Статистика приложения
    /// </summary>
    public interface IStatisticsService
    {
        #region Statistics Methods

        /// <summary>
        /// Получить статистику для администратора
        /// </summary>
        /// <param name="request">Опциональный период</param>
        /// <returns>Административная статистика</returns>
        Task<Result<GetAppGeneralStatisticsResponse>> GetStatistics(GetAppGeneralStatisticsRequest? request = null);

        /// <summary>
        /// Получить временную шкалу статистики для графиков
        /// </summary>
        /// <param name="request">Период</param>
        /// <returns>Точки шкалы</returns>
        Task<Result<GetAppStatisticsTimelineResponse>> GetStatisticsTimeline(GetAppStatisticsTimelineRequest request);

        #endregion Statistics Methods
    }
}
