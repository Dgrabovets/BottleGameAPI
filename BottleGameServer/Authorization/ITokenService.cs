using System.Security.Claims;

namespace BottleGameServer.Authorization
{
    /// <summary>
    /// Служит для создание токенов
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Генерирует токен
        /// </summary>
        string GenerateToken(IEnumerable<Claim> claims);
    }
}
