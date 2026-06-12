using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса администрирования
    /// </summary>
    public interface IAdministrationService
    {
        #region App Balance Methods

        /// <summary>
        /// Получить текущий баланс приложения
        /// </summary>
        /// <returns>Информация о балансе</returns>
        Task<Result<AppBalanceDTO>> GetAppBalance();

        /// <summary>
        /// Обновить нижний порог баланса приложения
        /// </summary>
        /// <param name="request">Запрос с новым порогом</param>
        /// <param name="adminId">ID администратора</param>
        /// <returns>Результат операции</returns>
        Task<Result> UpdateAppBalanceThreshold(UpdateAppBalanceThresholdRequest request, int adminId);

        /// <summary>
        /// Пополнить баланс приложения
        /// </summary>
        /// <param name="request">Запрос на пополнение</param>
        /// <param name="adminId">ID администратора</param>
        /// <returns>Результат операции с новым балансом</returns>
        Task<Result<decimal>> TopUpAppBalance(TopUpAppBalanceRequest request, int adminId);

        #endregion App Balance Methods

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

        #region Moderator Methods

        /// <summary>
        /// Зарегистрировать нового модератора
        /// </summary>
        /// <param name="request">Данные модератора</param>
        /// <returns>Результат операции</returns>
        Task<Result> RegisterModerator(AdministrationRoleUserCredentials request);

        /// <summary>
        /// Получить список модераторов или конкретного модератора
        /// </summary>
        /// <param name="moderatorId">ID модератора (опционально)</param>
        /// <returns>Список или один модератор</returns>
        Task<Result<object>> GetModerators(int? moderatorId);

        /// <summary>
        /// Удалить модератора
        /// </summary>
        /// <param name="moderatorId">ID модератора</param>
        /// <returns>Результат операции</returns>
        Task<Result> DeleteModerator(int moderatorId);

        #endregion Moderator Methods
    }
}



