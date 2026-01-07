using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using BottleGame.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// <inheritdoc cref="ITelegramAuthService"/>
    /// </summary>
    public class TelegramAuthService : ITelegramAuthService
    {
        #region Fields

        private readonly string _botToken;
        private readonly ILogger<TelegramAuthService> _logger;

        #endregion Fields

        #region Constructor

        public TelegramAuthService(IConfiguration config, ILogger<TelegramAuthService> logger)
        {
            _botToken = config["TelegramBotToken"]!;
            _logger = logger;
        }

        #endregion Constructor

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
                data.Remove("hash");

                var checkString = string.Join("\n", data.AllKeys.OrderBy(key => key).Select(key => $"{key}={data[key]}"));
                var hmacKey = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
                var secretKey = hmacKey.ComputeHash(Encoding.UTF8.GetBytes(_botToken));
                var hashKey = new HMACSHA256(secretKey);
                var hashBytes = hashKey.ComputeHash(Encoding.UTF8.GetBytes(checkString));
                var computedHash = Convert.ToHexStringLower(hashBytes);

                return computedHash.Equals(hash);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation error: {ex.Message}");
                return false;
            }
        }

        #endregion Methods
    }
}


