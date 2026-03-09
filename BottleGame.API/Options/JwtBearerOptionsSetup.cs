using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BottleGame.API.Options
{
    /// <summary>
    /// Настраивает JwtBearerOptions из валидированных JwtOptions (вызывается при старте приложения).
    /// </summary>
    public class JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
    {
        #region Поля

        /// <inheritdoc/>
        private readonly JwtOptions _jwtOptions = jwtOptions.Value;

        #endregion Поля

        #region Методы

        /// <inheritdoc/>
        public void Configure(string? name, JwtBearerOptions options)
        {
            // Настраиваем и когда name == null (общий Configure), и когда name == "Bearer"
            if (!string.IsNullOrEmpty(name) && name != JwtBearerDefaults.AuthenticationScheme)
                return;

            var key = _jwtOptions.Key;
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("JWT:Key не задан. Укажите переменную окружения JWT__Key или добавьте ключ в appsettings.Production.json.");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuerSigningKey = true,
            };
        }

        /// <inheritdoc/>
        public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);

        #endregion Методы
    }
}
