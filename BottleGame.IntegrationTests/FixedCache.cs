using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;

namespace BottleGame.IntegrationTests;

/// <summary>
/// Cache that always returns the same game parameters
/// </summary>
/// <param name="gameParams">Game params</param>
public sealed class FixedCache(GameParams gameParams) : ICacheService
{
    /// <summary>
    /// Returns the fixed game parameters
    /// </summary>
    /// <returns>The result of the operation</returns>
    public Task<GameParams?> GetGameParamsAsync() => Task.FromResult<GameParams?>(gameParams);

    /// <summary>
    /// Returns an empty list of transaction types
    /// </summary>
    /// <returns>The result of the operation</returns>
    public Task<Dictionary<int, string>> GetTransactionTypesAsync() => Task.FromResult(new Dictionary<int, string>());

    /// <summary>
    /// Returns an empty list of transaction statuses
    /// </summary>
    /// <returns>The result of the operation</returns>
    public Task<Dictionary<int, string>> GetTransactionStatusesAsync() => Task.FromResult(new Dictionary<int, string>());

    /// <summary>
    /// Does nothing: the parameters are fixed
    /// </summary>
    public void InvalidateGameParams() { }

    /// <summary>
    /// Does nothing: the statuses are not cached
    /// </summary>
    public void InvalidateTransactionStatuses() { }

    /// <summary>
    /// Does nothing: the types are not cached
    /// </summary>
    public void InvalidateTransactionTypes() { }
}
