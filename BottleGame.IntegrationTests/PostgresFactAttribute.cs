namespace BottleGame.IntegrationTests;

/// <summary>
/// Marks a test that needs PostgreSQL. Without the BOTTLEGAME_TEST_POSTGRES environment variable the test is skipped
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    /// <summary>
    /// Environment variable
    /// </summary>
    public const string ENVIRONMENT_VARIABLE = "BOTTLEGAME_TEST_POSTGRES";

    /// <summary>
    /// Skips the test when no PostgreSQL connection string is configured
    /// </summary>
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE)))
        {
            Skip = $"Set {ENVIRONMENT_VARIABLE}, for example Host=localhost;Port=5432;Username=postgres;Password=postgres";
        }
    }
}
