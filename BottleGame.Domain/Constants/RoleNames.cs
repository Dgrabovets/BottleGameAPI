namespace BottleGame.Domain.Constants
{
    /// <summary>
    /// Role names for authorization
    /// </summary>
    public static class RoleNames
    {
        /// <summary>
        /// Administrator role
        /// </summary>
        public const string Admin = "Admin";

        /// <summary>
        /// Moderator role
        /// </summary>
        public const string Moderator = "Moderator";

        /// <summary>
        /// Player role
        /// </summary>
        public const string Player = "Player";

        /// <summary>
        /// Role combination: Administrator and Moderator
        /// </summary>
        public const string AdminOrModerator = "Admin,Moderator";

        /// <summary>
        /// Role combination: Administrator and Player
        /// </summary>
        public const string AdminOrPlayer = "Admin,Player";

        /// <summary>
        /// Role combination: Administrator, Moderator and Player
        /// </summary>
        public const string All = "Admin,Moderator,Player";
    }
}
