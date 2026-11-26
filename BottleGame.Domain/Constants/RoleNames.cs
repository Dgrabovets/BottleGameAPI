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
        public const string ADMIN = "Admin";

        /// <summary>
        /// Moderator role
        /// </summary>
        public const string MODERATOR = "Moderator";

        /// <summary>
        /// Player role
        /// </summary>
        public const string PLAYER = "Player";

        /// <summary>
        /// Role combination: Administrator and Moderator
        /// </summary>
        public const string ADMIN_OR_MODERATOR = "Admin,Moderator";

        /// <summary>
        /// Role combination: Administrator and Player
        /// </summary>
        public const string ADMIN_OR_PLAYER = "Admin,Player";

        /// <summary>
        /// Role combination: Administrator, Moderator and Player
        /// </summary>
        public const string ALL = "Admin,Moderator,Player";
    }
}
