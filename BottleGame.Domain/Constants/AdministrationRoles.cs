namespace BottleGame.Domain.Constants
{
    /// <summary>
    /// Administrative roles
    /// </summary>
    public static class AdministrationRoles
    {
        /// <summary>
        /// Administrator (full access)
        /// </summary>
        public const int ADMIN = 1;

        /// <summary>
        /// Moderator (limited access)
        /// </summary>
        public const int MODERATOR = 2;

        /// <summary>
        /// Role name for JWT claims
        /// </summary>
        public static string GetRoleName(int roleId) => roleId switch
        {
            ADMIN => "Admin",
            MODERATOR => "Moderator",
            _ => "Unknown"
        };
    }
}
