using Microsoft.Extensions.Options;

namespace BottleGame.Infrastructure.External.Processing
{
    /// <summary>
    /// Проверяет настройки провайдера: в режиме Live обязательны реквизиты магазина и адрес возврата.
    /// </summary>
    public class ProcessingOptionsValidator : IValidateOptions<ProcessingOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, ProcessingOptions options)
        {
            if (options.Mode != ProcessingMode.Live)
            {
                return ValidateOptionsResult.Success;
            }

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(options.ShopId))
            {
                errors.Add("Processing:ShopId обязателен в режиме Live. Задайте переменную окружения Processing__ShopId.");
            }

            if (string.IsNullOrWhiteSpace(options.SecretKey))
            {
                errors.Add("Processing:SecretKey обязателен в режиме Live. Задайте переменную окружения Processing__SecretKey.");
            }

            if (!Uri.TryCreate(options.ReturnUrl, UriKind.Absolute, out _))
            {
                errors.Add("Processing:ReturnUrl обязателен в режиме Live и должен быть абсолютным адресом.");
            }

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}
