using BottleGame.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// <inheritdoc cref="ITelegramAuthService"/>
    /// </summary>
    public class TelegramAuthService(IConfiguration config, ILogger<TelegramAuthService> logger) : ITelegramAuthService
    {
        #region Fields

        /// <summary>
        /// Token of the Telegram bot used to verify initData
        /// </summary>
        private readonly string _botToken = config["TelegramBotToken"]!;

        /// <summary>
        /// How long initData stays valid after Telegram signed it, in minutes. Zero turns the check off
        /// </summary>
        private readonly int _maxAgeMinutes = config.GetValue("Telegram:InitDataMaxAgeMinutes", 1440);

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<TelegramAuthService> _logger = logger;

        #endregion Fields

        #region Methods

        /// <summary>
        /// <inheritdoc cref="ITelegramAuthService.ValidateInitData(string)"/>
        /// </summary>
        public bool ValidateInitData(string initData)
        {
            try
            {
                NameValueCollection data = HttpUtility.ParseQueryString(initData);

                var hash = data["hash"];
                if (string.IsNullOrEmpty(hash))
                {
                    return false;
                }

                data.Remove("hash");

                var checkString = string.Join("\n", data.AllKeys.OrderBy(key => key).Select(key => $"{key}={data[key]}"));
                using var hmacKey = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
                var secretKey = hmacKey.ComputeHash(Encoding.UTF8.GetBytes(_botToken));
                using var hashKey = new HMACSHA256(secretKey);
                var computed = hashKey.ComputeHash(Encoding.UTF8.GetBytes(checkString));

                // Compare in constant time, so the response time does not reveal how many bytes matched
                if (!CryptographicOperations.FixedTimeEquals(computed, Convert.FromHexString(hash)))
                {
                    return false;
                }

                // A signature never expires on its own, so an old initData could be replayed for ever
                return IsFresh(data["auth_date"]);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                _logger.LogWarning(ex, "Telegram initData could not be validated");
                return false;
            }
        }

        /// <summary>
        /// Checks that Telegram signed the data recently
        /// </summary>
        /// <param name="authDate">auth_date field of initData: Unix time in seconds</param>
        /// <returns>true if the data is not older than the allowed age</returns>
        private bool IsFresh(string? authDate)
        {
            if (_maxAgeMinutes <= 0)
            {
                return true;
            }

            if (!long.TryParse(authDate, out var seconds))
            {
                return false;
            }

            var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(seconds);
            return age <= TimeSpan.FromMinutes(_maxAgeMinutes) && age >= TimeSpan.FromMinutes(-5);
        }

        #endregion Methods
    }
}
