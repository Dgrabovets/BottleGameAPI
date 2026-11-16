using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BottleGame.API.Options
{
    /// <summary>
    /// Configures JwtBearerOptions from the validated JwtOptions (called when the application starts)
    /// </summary>
    public class JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
    {
        #region Fields

        /// <inheritdoc/>
        private readonly JwtOptions _jwtOptions = jwtOptions.Value;

        #endregion Fields

        #region Methods

        /// <inheritdoc/>
        public void Configure(string? name, JwtBearerOptions options)
        {
            // Configure both when name == null (the general Configure) and when name == "Bearer"
            if (!string.IsNullOrEmpty(name) && name != JwtBearerDefaults.AuthenticationScheme)
                return;

            var key = _jwtOptions.Key;
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("JWT:Key is not set. Set the JWT__Key environment variable or add the key to appsettings.Production.json.");

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

        #endregion Methods
    }
}
