using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BottleGame.Application
{
    /// <summary>
    /// Extensions for registering the services of the Application layer
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds the services of the Application layer to the DI container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // FluentValidation
            services.AddValidatorsFromAssemblyContaining<Validators.AdministrationRoleUserCredentialsValidator>();

            return services;
        }
    }
}

