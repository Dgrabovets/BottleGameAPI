using BottleGame.Domain.Entities;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса кэширования справочных данных
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Получить игровые параметры из кэша
        /// </summary>
        /// <returns>Игровые параметры</returns>
        Task<GameParams?> GetGameParamsAsync();

        /// <summary>
        /// Получить все типы транзакций из кэша
        /// </summary>
        /// <returns>Словарь типов транзакций (Id -> Name)</returns>
        Task<Dictionary<int, string>> GetTransactionTypesAsync();

        /// <summary>
        /// Получить все статусы транзакций из кэша
        /// </summary>
        /// <returns>Словарь статусов транзакций (Id -> Name)</returns>
        Task<Dictionary<int, string>> GetTransactionStatusesAsync();

        /// <summary>
        /// Инвалидировать кэш игровых параметров
        /// </summary>
        void InvalidateGameParams();

        /// <summary>
        /// Инвалидировать кэш типов транзакций
        /// </summary>
        void InvalidateTransactionTypes();

        /// <summary>
        /// Инвалидировать кэш статусов транзакций
        /// </summary>
        void InvalidateTransactionStatuses();
    }
}



