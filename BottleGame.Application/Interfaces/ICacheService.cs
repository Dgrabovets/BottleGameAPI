using BottleGame.Domain.Entities;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Interface of the service that caches reference data
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Gets the game parameters from the cache
        /// </summary>
        /// <returns>Game parameters</returns>
        Task<GameParams?> GetGameParamsAsync();

        /// <summary>
        /// Gets all transaction types from the cache
        /// </summary>
        /// <returns>Dictionary of transaction types (Id -> Name)</returns>
        Task<Dictionary<int, string>> GetTransactionTypesAsync();

        /// <summary>
        /// Gets all transaction statuses from the cache
        /// </summary>
        /// <returns>Dictionary of transaction statuses (Id -> Name)</returns>
        Task<Dictionary<int, string>> GetTransactionStatusesAsync();

        /// <summary>
        /// Invalidates the cache of the game parameters
        /// </summary>
        void InvalidateGameParams();

        /// <summary>
        /// Invalidates the cache of the transaction types
        /// </summary>
        void InvalidateTransactionTypes();

        /// <summary>
        /// Invalidates the cache of the transaction statuses
        /// </summary>
        void InvalidateTransactionStatuses();
    }
}



