using BottleGame.Domain.Enums;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BottleGame.IntegrationTests;

/// <summary>
/// Integration tests of the SQL scripts: database/schema.sql matches the EF model and the upgrade scripts bring an older database to it
/// </summary>
public class DatabaseScriptsIntegrationTests
{
    /// <summary>
    /// Reads a script from the repository's database folder
    /// </summary>
    /// <param name="relativePath">Path inside the database folder</param>
    /// <returns>Text of the script</returns>
    private static string ReadDatabaseScript(string relativePath) =>
        File.ReadAllText(Path.Combine(PostgresFixture.FindDatabaseFolder(), relativePath));

    /// <summary>
    /// Creates a throwaway database and runs the given scripts on it
    /// </summary>
    /// <param name="scripts">Scripts to run in order</param>
    /// <returns>Connection string of the new database</returns>
    private static async Task<string> CreateDatabaseAsync(params string[] scripts)
    {
        var serverConnection = Environment.GetEnvironmentVariable(PostgresFactAttribute.ENVIRONMENT_VARIABLE)!;
        var databaseName = $"bottle_it_{Guid.NewGuid():N}";
        await PostgresFixture.ExecuteAsync(serverConnection, $"CREATE DATABASE \"{databaseName}\"");

        var databaseConnection = new NpgsqlConnectionStringBuilder(serverConnection) { Database = databaseName }.ConnectionString;
        foreach (var script in scripts)
        {
            await PostgresFixture.ExecuteAsync(databaseConnection, script);
        }

        return databaseConnection;
    }

    /// <summary>
    /// Drops a throwaway database
    /// </summary>
    /// <param name="databaseConnection">Connection string of the database</param>
    private static async Task DropDatabaseAsync(string databaseConnection)
    {
        var serverConnection = Environment.GetEnvironmentVariable(PostgresFactAttribute.ENVIRONMENT_VARIABLE)!;
        var databaseName = new NpgsqlConnectionStringBuilder(databaseConnection).Database;
        await PostgresFixture.ExecuteAsync(serverConnection, $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)");
    }

    /// <summary>
    /// Creates a database context on the given database
    /// </summary>
    /// <param name="databaseConnection">Connection string of the database</param>
    /// <returns>The created value</returns>
    private static ApplicationDbContext CreateContext(string databaseConnection) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(databaseConnection).Options);

    /// <summary>
    /// Describes every column of the public schema: table, name, type, length, nullability, identity and default
    /// </summary>
    /// <param name="databaseConnection">Connection string of the database</param>
    /// <returns>One line per column, sorted</returns>
    private static async Task<List<string>> ReadColumnsAsync(string databaseConnection)
    {
        const string sql = """
            SELECT table_name || '.' || column_name || ' ' || data_type
                   || coalesce('(' || character_maximum_length || ')', '')
                   || CASE WHEN is_nullable = 'YES' THEN ' null' ELSE ' not null' END
                   || coalesce(' identity ' || identity_generation, '')
                   || coalesce(' default ' || column_default, '')
            FROM information_schema.columns
            WHERE table_schema = 'public'
            ORDER BY 1
            """;

        await using var connection = new NpgsqlConnection(databaseConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    /// <summary>
    /// schema.sql creates the same columns as the EF model, with the same types, lengths, nullability and defaults
    /// </summary>
    [PostgresFact]
    public async Task SchemaScript_MatchesModel()
    {
        var fromScript = await CreateDatabaseAsync(ReadDatabaseScript("schema.sql"));
        var fromModel = await CreateDatabaseAsync();

        try
        {
            await using (var context = CreateContext(fromModel))
            {
                await context.Database.EnsureCreatedAsync();
            }

            Assert.Equal(await ReadColumnsAsync(fromModel), await ReadColumnsAsync(fromScript));
        }
        finally
        {
            await DropDatabaseAsync(fromScript);
            await DropDatabaseAsync(fromModel);
        }
    }

    /// <summary>
    /// The upgrade script brings a database of the previous schema to the current one, turns the old marker sentence into the payout state,
    /// keeps real error texts and can be run again
    /// </summary>
    [PostgresFact]
    public async Task PayoutStateUpgrade_ConvertsPreviousDatabase_AndCanRunTwice()
    {
        // The sentence the previous version wrote into ProcessingError, as existing rows hold it
        const string oldRows = """
            INSERT INTO "Players" ("Id", "TelegramId", "Name", "AvatarUrl", "RegisteredInAppAt")
            VALUES (1, 1, 'Player', 'https://example.com/avatar.jpg', now());

            INSERT INTO "MoneyTransactions" ("Id", "PlayerId", "StatusId", "TransactionTypeId", "Amount", "PaymentGateway", "Currency", "ProcessingError", "CreatedAt")
            VALUES
                (1, 1, 1, 2, 300, 'sbp', 'rub', 'Payout outcome unknown: wait for the YooKassa notification or approve again to repeat the request with the same idempotence key', now()),
                (2, 1, 3, 2, 200, 'sbp', 'rub', 'payout_network/recipient_not_found', now()),
                (3, 1, 1, 2, 100, NULL, NULL, NULL, now());
            """;

        var upgrade = ReadDatabaseScript(Path.Combine("upgrade", "001-payout-state.sql"));
        var previousSchema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "schema-before-payout-state.sql"));
        var upgraded = await CreateDatabaseAsync(previousSchema, ReadDatabaseScript("seed.sql"), oldRows);
        var current = await CreateDatabaseAsync(ReadDatabaseScript("schema.sql"));

        try
        {
            await PostgresFixture.ExecuteAsync(upgraded, upgrade);
            await PostgresFixture.ExecuteAsync(upgraded, upgrade);

            Assert.Equal(await ReadColumnsAsync(current), await ReadColumnsAsync(upgraded));

            await using var context = CreateContext(upgraded);
            var rows = await context.MoneyTransactions.AsNoTracking().OrderBy(t => t.Id).ToListAsync();

            Assert.Equal([PayoutState.OutcomeUnknown, PayoutState.NotRequested, PayoutState.NotRequested], rows.Select(t => t.PayoutState));
            Assert.Equal([null, "payout_network/recipient_not_found", null], rows.Select(t => t.ProcessingError));
            Assert.All(rows, t => Assert.Null(t.PayoutRequestedAt));
            Assert.All(rows, t => Assert.Null(t.PayoutWallet));
        }
        finally
        {
            await DropDatabaseAsync(upgraded);
            await DropDatabaseAsync(current);
        }
    }
}
