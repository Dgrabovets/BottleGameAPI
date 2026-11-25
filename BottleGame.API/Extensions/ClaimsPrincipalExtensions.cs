using BottleGame.Domain.Constants;
using System.Security.Claims;

namespace BottleGame.API.Extensions
{
    /// <summary>
    /// Questions about the signed-in user, answered from the claims
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Gets the id of the signed-in user
        /// </summary>
        /// <param name="user">Signed-in user</param>
        /// <returns>Id or null if the claim is missing or is not a number</returns>
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            return int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
                ? userId
                : null;
        }

        /// <summary>
        /// Checks whether the user is staff
        /// </summary>
        /// <param name="user">Signed-in user</param>
        /// <returns>true for an administrator or a moderator</returns>
        public static bool IsStaff(this ClaimsPrincipal user)
        {
            return user.IsInRole(RoleNames.Admin) || user.IsInRole(RoleNames.Moderator);
        }

        /// <summary>
        /// Checks whether the user may see the data of a player
        /// </summary>
        /// <param name="user">Signed-in user</param>
        /// <param name="playerId">Player id</param>
        /// <returns>true for staff and for the player themself</returns>
        public static bool HasAccessToPlayer(this ClaimsPrincipal user, int playerId)
        {
            return user.IsStaff() || user.GetUserId() == playerId;
        }
    }
}
