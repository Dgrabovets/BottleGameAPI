using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BottleGame.IntegrationTests;

/// <summary>
/// Creates a throwaway database from database/schema.sql and database/seed.sql for a set of tests and drops it afterwards
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>
    /// Server connection
    /// </summary>
    private string? _serverConnection;

    /// <summary>
    /// Database name
    /// </summary>
    private string? _databaseName;

    /// <summary>
    /// Database connection
    /// </summary>
    private string? _databaseConnection;

    /// <summary>
    /// Telegram id counter
    /// </summary>
    private int _telegramIdCounter;

    /// <summary>
    /// Creates the throwaway database and loads the SQL scripts
    /// </summary>
    public async Task InitializeAsync()
    {
        _serverConnection = Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(_serverConnection))
        {
            return;
        }

        _databaseName = $"bottle_it_{Guid.NewGuid():N}";
        await ExecuteAsync(_serverConnection, $"CREATE DATABASE \"{_databaseName}\"");

        _databaseConnection = new NpgsqlConnectionStringBuilder(_serverConnection) { Database = _databaseName }.ConnectionString;

        var databaseFolder = FindDatabaseFolder();
        await ExecuteAsync(_databaseConnection, await File.ReadAllTextAsync(Path.Combine(databaseFolder, "schema.sql")));
        await ExecuteAsync(_databaseConnection, await File.ReadAllTextAsync(Path.Combine(databaseFolder, "seed.sql")));
    }

    /// <summary>
    /// Drops the throwaway database
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_serverConnection is null || _databaseName is null)
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        await ExecuteAsync(_serverConnection, $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)");
    }

    /// <summary>
    /// Creates a new database context on the throwaway database
    /// </summary>
    /// <returns>The created value</returns>
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_databaseConnection ?? throw new InvalidOperationException("The database was not created: set the environment variable"))
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Adds a player with a balance, options and round statistics and returns the player id
    /// </summary>
    /// <param name="balance">Balance</param>
    /// <returns>The result of the operation</returns>
    public async Task<int> AddPlayerAsync(decimal balance = 1000)
    {
        await using var context = CreateContext();

        var player = new Player
        {
            TelegramId = 1_000_000 + Interlocked.Increment(ref _telegramIdCounter),
            Name = "Integration player",
            AvatarUrl = "https://example.com/avatar.jpg",
            RegisteredInAppAt = DateTime.UtcNow
        };

        context.Players.Add(player);
        await context.SaveChangesAsync();

        context.PlayersBalances.Add(new Balance { PlayerId = player.Id, Amount = balance });
        context.PlayersOptions.Add(new PlayerOptions
        {
            PlayerId = player.Id,
            WinChance = 0.5f,
            Currency = "rub",
            PaymentMethod = "yookassa",
            PayoutMethod = "sbp",
            Wallet = "79001234567:100000000111"
        });

        context.PlayersPlayedRoundsStatisticForWithdrawals.Add(new PlayerPlayedRoundsStatisticForWithdrawal { PlayerId = player.Id, PlayedRoundsCount = 10 });
        await context.SaveChangesAsync();

        return player.Id;
    }

    /// <summary>
    /// Adds a pending transaction and returns its id
    /// </summary>
    /// <param name="playerId">Player id</param>
    /// <param name="typeId">Type id</param>
    /// <param name="amount">Amount</param>
    /// <returns>The result of the operation</returns>
    public async Task<int> AddPendingTransactionAsync(int playerId, int typeId, decimal amount)
    {
        await using var context = CreateContext();

        var transaction = new MoneyTransaction
        {
            PlayerId = playerId,
            Amount = amount,
            TransactionTypeId = typeId,
            TransactionStatusId = Domain.Constants.TransactionStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.MoneyTransactions.Add(transaction);
        await context.SaveChangesAsync();

        return transaction.Id;
    }

    /// <summary>
    /// Runs a SQL script on the given connection
    /// </summary>
    /// <param name="connectionString">Connection string</param>
    /// <param name="sql">Sql</param>
    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Finds the repository's database folder by walking up from the test output
    /// </summary>
    /// <returns>The created value</returns>
    private static string FindDatabaseFolder()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "database");
            if (File.Exists(Path.Combine(candidate, "schema.sql")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("The database folder with schema.sql was not found above the test output directory");
    }
}
