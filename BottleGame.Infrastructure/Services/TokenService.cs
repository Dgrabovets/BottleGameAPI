using BottleGame.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// <inheritdoc cref="ITokenService"/>
    /// </summary>
    /// <param name="configuration">Configuration</param>
    public class TokenService(IConfiguration configuration) : ITokenService
    {
        #region Fields

        /// <summary>
        /// Configuration
        /// </summary>
        private readonly IConfiguration _configuration = configuration;

        #endregion Fields

        #region Methods

        /// <summary>
        /// <inheritdoc cref="ITokenService.GenerateToken(IEnumerable{Claim})"/>
        /// </summary>
        public string GenerateToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpireMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion Methods
    }
}
