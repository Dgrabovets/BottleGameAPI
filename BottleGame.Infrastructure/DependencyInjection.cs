using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.External.Processing;
using BottleGame.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            // База данных
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgresConnection")));

            // Кэширование
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, CacheService>();

            // Сервисы авторизации
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITelegramAuthService, TelegramAuthService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            // Processing (платёжная система)
            services.Configure<ProcessingOptions>(configuration.GetSection("Processing"));
            services.AddHttpClient<IProcessingService, ProcessingService>();
            services.AddScoped<ICurrencyService, CurrencyService>();

            // Бизнес-сервисы
            services.AddScoped<IPlayerService, PlayerService>();
            services.AddScoped<IBanService, BanService>();
            services.AddScoped<IGameRoundService, GameRoundService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IAdministrationService, AdministrationService>();

            return services;
        }
    }
}

