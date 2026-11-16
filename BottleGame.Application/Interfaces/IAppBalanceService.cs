using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Баланс приложения
    /// </summary>
    public interface IAppBalanceService
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
    }
}
