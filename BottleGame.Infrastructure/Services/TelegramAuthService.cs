using BottleGame.Application.DTOs;
using BottleGame.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// <inheritdoc cref="ITelegramAuthService"/>
    /// </summary>
    public class TelegramAuthService : ITelegramAuthService
    {
        #region Fields

        /// <summary>
        /// Token of the Telegram bot used to verify initData
        /// </summary>
        private readonly string? _botToken;

        /// <summary>
        /// How long initData stays valid after Telegram signed it, in minutes. Zero turns the check off
        /// </summary>
        private readonly int _maxAgeMinutes;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<TelegramAuthService> _logger;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="TelegramAuthService"/>
        /// </summary>
        /// <param name="config">Application configuration</param>
        /// <param name="logger">Logger</param>
        public TelegramAuthService(IConfiguration config, ILogger<TelegramAuthService> logger)
        {
            _botToken = config["TelegramBotToken"];
            _maxAgeMinutes = config.GetValue("Telegram:InitDataMaxAgeMinutes", 1440);
            _logger = logger;
        }

        #region Methods

        /// <summary>
        /// <inheritdoc cref="ITelegramAuthService.ValidateInitData(string)"/>
        /// </summary>
        public bool ValidateInitData(string initData)
        {
            // With an empty key anyone can compute the signature. Startup refuses such a token, this is the second line
            if (string.IsNullOrEmpty(_botToken))
            {
                return false;
            }

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
        /// <inheritdoc cref="ITelegramAuthService.ParseInitData(string)"/>
        /// </summary>
        public ParsedTelegramUser ParseInitData(string initData)
        {
            var result = new ParsedTelegramUser();
            var query = HttpUtility.ParseQueryString(initData);

            // Extract the user data from the query string
            result.TelegramId = long.Parse(GetJsonValue(query["user"]!, "id")!);
            result.Username = GetJsonValue(query["user"]!, "username") ?? GetJsonValue(query["user"]!, "first_name")!;
            result.PhotoUrl = GetJsonValue(query["user"]!, "photo_url") ?? string.Empty;
            var startParts = query["start_param"]?.Split('_');

            if (startParts is { Length: > 1 } && int.TryParse(startParts[1], out var invitingPlayerId))
            {
                result.InvitingPlayerId = invitingPlayerId;
            }

            // Parse the date (Unix timestamp)
            result.RegisteredAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(query["auth_date"]!)).UtcDateTime;

            return result;
        }

        /// <summary>
        /// Gets a property of a JSON object as text
        /// </summary>
        /// <param name="json">JSON object</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>Value or null if the property is missing or not a simple value</returns>
        private static string? GetJsonValue(string json, string propertyName)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString(),
                    JsonValueKind.Number => property.GetInt64().ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => null
                };
            }

            return null;
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
