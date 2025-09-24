using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BottleGameServer.Authorization
{
    /// <summary>
    /// <inheritdoc cref="ITokenService"/>
    /// </summary>
    public class TokenService : ITokenService
    {
        #region Fields
        
        private readonly IConfiguration _configuration;

        #endregion Fields

        #region Constructor

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #endregion Constructor

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
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpireMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion Methods
    }
}
