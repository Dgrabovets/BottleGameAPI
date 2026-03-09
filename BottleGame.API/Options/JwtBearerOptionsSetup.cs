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
            if (name != JwtBearerDefaults.AuthenticationScheme)
                return;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
                ValidateIssuerSigningKey = true,
            };
        }

        /// <inheritdoc/>
        public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);

        #endregion Методы
    }
}
