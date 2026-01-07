namespace BottleGame.Domain.Constants
{
    /// <summary>
    /// Названия ролей для авторизации.
    /// </summary>
    public static class RoleNames
    {
        /// <summary>
        /// Роль администратора.
        /// </summary>
        public const string Admin = "Admin";

        /// <summary>
        /// Роль модератора.
        /// </summary>
        public const string Moderator = "Moderator";

        /// <summary>
        /// Роль игрока.
        /// </summary>
        public const string Player = "Player";

        /// <summary>
        /// Комбинация ролей: Администратор и Модератор.
        /// </summary>
        public const string AdminOrModerator = "Admin,Moderator";

        /// <summary>
        /// Комбинация ролей: Администратор и Игрок.
        /// </summary>
        public const string AdminOrPlayer = "Admin,Player";

        /// <summary>
        /// Комбинация ролей: Администратор, Модератор и Игрок.
        /// </summary>
        public const string All = "Admin,Moderator,Player";
    }
}


