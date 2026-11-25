using BottleGame.Domain.Entities;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Service that moves password hashes of the older format to the current one
    /// </summary>
    public interface IPasswordUpgradeService
    {
        /// <summary>
        /// Rehashes the password of a staff user who has just signed in, if the stored hash is old
        /// </summary>
        /// <param name="admin">Staff user</param>
        /// <param name="password">Password that has been verified</param>
        /// <remarks>
        /// A failure is logged and does not stop the sign-in
        /// </remarks>
        Task UpgradeIfNeededAsync(AdministrationRoleUser admin, string password);
    }
}
