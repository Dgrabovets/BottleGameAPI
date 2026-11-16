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
    /// Расширения для регистрации сервисов Infrastructure слоя
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Добавляет сервисы Infrastructure слоя в контейнер DI
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <returns>Коллекция сервисов</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PostgresConnection");
            if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("ENV_", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Строка подключения к БД не задана. Укажите переменную окружения ConnectionStrings__PostgresConnection или добавьте секрет POSTGRES_CONNECTION в GitHub Actions.");

            // База данных
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Кэширование
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, CacheService>();

            // Сервисы авторизации
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITelegramAuthService, TelegramAuthService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            // Платёжный провайдер (ЮKassa) — валидация при старте приложения
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

            // Бизнес-сервисы
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

