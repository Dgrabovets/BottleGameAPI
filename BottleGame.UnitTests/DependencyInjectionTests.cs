using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure;
using BottleGame.Infrastructure.Services.Payments;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BottleGame.UnitTests;

/// <summary>
/// Checks that the infrastructure registrations build and choose the processor by processing mode.
/// </summary>
public class DependencyInjectionTests
{
    /// <summary>
    /// Builds a service provider with the given processing mode.
    /// </summary>
    /// <param name="mode">Mode.</param>
    /// <returns>The created value.</returns>
    private static ServiceProvider BuildProvider(string mode)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Processing:Mode"] = mode,
                ["Processing:ShopId"] = "shop",
                ["Processing:SecretKey"] = "secret",
                ["Processing:ReturnUrl"] = "https://example.com/return",
                ["JWT:Key"] = "0123456789abcdef0123456789abcdef0123",
                ["Security:PasswordHashSecretKey"] = "secret"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
    }

    /// <summary>
    /// The processing mode selects the matching transaction processor.
    /// </summary>
    /// <param name="mode">Mode.</param>
    /// <param name="expected">Expected.</param>
    [Theory]
    [InlineData("Simulated", typeof(SimulatedTransactionProcessor))]
    [InlineData("Live", typeof(LiveTransactionProcessor))]
    public void TransactionProcessor_IsChosenByProcessingMode(string mode, Type expected)
    {
        using var provider = BuildProvider(mode);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITransactionProcessor>().Should().BeOfType(expected);
    }

    /// <summary>
    /// The application services can be resolved from the container.
    /// </summary>
    /// <param name="serviceType">Service type.</param>
    [Theory]
    [InlineData(typeof(ITransactionService))]
    [InlineData(typeof(IPlayerService))]
    [InlineData(typeof(IPlayerStatisticsService))]
    [InlineData(typeof(IAppBalanceService))]
    [InlineData(typeof(IStatisticsService))]
    [InlineData(typeof(IModeratorService))]
    [InlineData(typeof(IPaymentWebhookService))]
    [InlineData(typeof(ICurrencyService))]
    public void ApplicationServices_CanBeResolved(Type serviceType)
    {
        using var provider = BuildProvider("Live");
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService(serviceType).Should().NotBeNull();
    }
}
