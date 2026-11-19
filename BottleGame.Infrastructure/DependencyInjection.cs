using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.External.Processing;
using BottleGame.Infrastructure.External.YooKassa;
using BottleGame.Infrastructure.Services;
using BottleGame.Infrastructure.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BottleGame.Infrastructure
{
    /// <summary>
    /// Extensions for registering the services of the Infrastructure layer
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds the services of the Infrastructure layer to the DI container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PostgresConnection");
            if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("ENV_", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Database connection string is not set. Set the environment variable ConnectionStrings__PostgresConnection or add the POSTGRES_CONNECTION secret to GitHub Actions.");

            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Caching
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, CacheService>();

            // Authorization services
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITelegramAuthService, TelegramAuthService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            // Payment provider (YooKassa) — validation at application startup
            services.AddOptions<ProcessingOptions>()
                .Bind(configuration.GetSection("Processing"))
                .ValidateOnStart();

            services.AddSingleton<IValidateOptions<ProcessingOptions>, ProcessingOptionsValidator>();
            services.AddHttpClient<IPaymentProvider, YooKassaPaymentProvider>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<ProcessingOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

            services.AddScoped<ICurrencyService, CurrencyService>();

            // Business services
            services.AddScoped<IPlayerService, PlayerService>();
            services.AddScoped<IPlayerStatisticsService, PlayerStatisticsService>();
            services.AddScoped<IBanService, BanService>();
            services.AddScoped<IGameRoundService, GameRoundService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<ITransactionProcessor>(provider =>
                provider.GetRequiredService<IOptions<ProcessingOptions>>().Value.Mode == ProcessingMode.Live
                    ? ActivatorUtilities.CreateInstance<LiveTransactionProcessor>(provider)
                    : ActivatorUtilities.CreateInstance<SimulatedTransactionProcessor>(provider));

            services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
            services.AddScoped<IAppBalanceService, AppBalanceService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<IModeratorService, ModeratorService>();

            return services;
        }
    }
}
