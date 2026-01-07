namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Модель учетных данных для административных ролей
    /// </summary>
    public class AdministrationRoleUserCredentials
    {
        /// <summary>
        /// Логин
        /// </summary>
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Пароль
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
