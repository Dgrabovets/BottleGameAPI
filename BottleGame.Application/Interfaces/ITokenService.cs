using System.Security.Claims;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Service for generating JWT tokens
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token
        /// </summary>
        string GenerateToken(IEnumerable<Claim> claims);
    }
}
