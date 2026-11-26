using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;

namespace BottleGame.IntegrationTests;

/// <summary>
/// Cache that always returns the same game parameters
/// </summary>
public sealed class FixedCache : ICacheService
{
    /// <summary>
    /// Game parameters returned by the cache
    /// </summary>
    private readonly GameParams _gameParams;

    /// <summary>
    /// Initializes a new instance of <see cref="FixedCache"/>
    /// </summary>
    /// <param name="gameParams">Game params</param>
    public FixedCache(GameParams gameParams)
    {
        _gameParams = gameParams;
    }

    /// <summary>
    /// Returns the fixed game parameters
    /// </summary>
    /// <returns>The result of the operation</returns>
    public Task<GameParams?> GetGameParamsAsync() => Task.FromResult<GameParams?>(_gameParams);

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
