using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BottleGame.Application
{
    /// <summary>
    /// Расширения для регистрации сервисов Application слоя
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Добавляет сервисы Application слоя в контейнер DI
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <returns>Коллекция сервисов</returns>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // FluentValidation
            services.AddValidatorsFromAssemblyContaining<Validators.AdministrationRoleUserCredentialsValidator>();

            return services;
        }
    }
}

