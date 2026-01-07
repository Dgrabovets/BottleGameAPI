using System.Security.Claims;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Сервис для генерации JWT токенов
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Генерирует JWT токен
        /// </summary>
        string GenerateToken(IEnumerable<Claim> claims);
    }
}

