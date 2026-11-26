using BottleGame.Application.DTOs.Requests;
using BottleGame.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BottleGame.IntegrationTests;

/// <summary>
/// Integration tests of bans on a real PostgreSQL database: the history row needs a version that only the database generates
/// </summary>
public class BanServiceIntegrationTests : IClassFixture<PostgresFixture>
{
    /// <summary>
    /// Database
    /// </summary>
    private readonly PostgresFixture _database;

    /// <summary>
    /// Initializes a new instance of <see cref="BanServiceIntegrationTests"/>
    /// </summary>
    /// <param name="database">Database</param>
    public BanServiceIntegrationTests(PostgresFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Banning creates an active ban and writes it to the ban history
    /// </summary>
    [PostgresFact]
    public async Task BanPlayer_Success_CreatesBanAndHistory()
    {
        var playerId = await _database.AddPlayerAsync();

        await using (var context = _database.CreateContext())
        {
            var service = new BanService(context, NullLogger<BanService>.Instance);

            var result = await service.BanPlayer(new BanPlayerRequest { PlayerId = playerId, BanReason = "Test ban" });

            Assert.True(result.IsSuccess, result.Error);
        }

        await using var check = _database.CreateContext();
        var ban = await check.Bans.SingleAsync(b => b.PlayerId == playerId);

        Assert.True(ban.IsActive);
        Assert.Equal("Test ban", ban.BanReason);
        Assert.Equal(1, await check.BansHistory.CountAsync(history => history.PlayerId == playerId));
    }
}
