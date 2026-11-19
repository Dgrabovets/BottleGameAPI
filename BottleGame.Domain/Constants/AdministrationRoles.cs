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
        public const int Admin = 1;

        /// <summary>
        /// Moderator (limited access)
        /// </summary>
        public const int Moderator = 2;

        /// <summary>
        /// Role name for JWT claims
        /// </summary>
        public static string GetRoleName(int roleId) => roleId switch
        {
            Admin => "Admin",
            Moderator => "Moderator",
            _ => "Unknown"
        };
    }
}
