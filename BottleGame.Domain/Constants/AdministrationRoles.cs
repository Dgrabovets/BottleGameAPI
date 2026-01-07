namespace BottleGame.Domain.Constants
{
    /// <summary>
    /// Административные роли
    /// </summary>
    public static class AdministrationRoles
    {
        /// <summary>
        /// Администратор (полный доступ)
        /// </summary>
        public const int Admin = 1;

        /// <summary>
        /// Модератор (ограниченный доступ)
        /// </summary>
        public const int Moderator = 2;

        /// <summary>
        /// Название роли для JWT Claims
        /// </summary>
        public static string GetRoleName(int roleId) => roleId switch
        {
            Admin => "Admin",
            Moderator => "Moderator",
            _ => "Unknown"
        };
    }
}


