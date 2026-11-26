using Microsoft.Extensions.Options;

namespace BottleGame.API.Options
{
    /// <summary>
    /// Validates the bot token: a token that anyone can know would let anyone sign initData and sign in as any player
    /// </summary>
    /// <param name="environment">Hosting environment, the docker-compose placeholder is allowed only in Development</param>
    public class TelegramOptionsValidator(IHostEnvironment environment) : IValidateOptions<TelegramOptions>
    {
        #region Fields

        /// <summary>
        /// Start of the development placeholders in docker-compose.yml
        /// </summary>
        public const string ComposePlaceholderPrefix = "local-development";

        /// <summary>
        /// Hosting environment
        /// </summary>
        private readonly IHostEnvironment _environment = environment;

        #endregion Fields

        #region Methods

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, TelegramOptions options)
        {
            var token = options.BotToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                return ValidateOptionsResult.Fail("TelegramBotToken is required. Set the TelegramBotToken environment variable or the TELEGRAMBOTTOKEN secret in GitHub Actions.");
            }

            if (token.StartsWith("ENV_", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateOptionsResult.Fail("TelegramBotToken still holds the ENV_ placeholder from appsettings.json. Set the TelegramBotToken environment variable.");
            }

            if (!_environment.IsDevelopment() && token.StartsWith(ComposePlaceholderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return ValidateOptionsResult.Fail("TelegramBotToken holds the docker-compose placeholder, which is allowed only in Development. Set the real bot token.");
            }

            return ValidateOptionsResult.Success;
        }

        #endregion Methods
    }
}
