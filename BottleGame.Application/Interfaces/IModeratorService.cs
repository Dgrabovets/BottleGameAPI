using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Управление модераторами
    /// </summary>
    public interface IModeratorService
    {
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
