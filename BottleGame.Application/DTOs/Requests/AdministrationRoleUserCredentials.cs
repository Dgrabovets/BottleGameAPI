namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Credentials model for administration roles
    /// </summary>
    public class AdministrationRoleUserCredentials
    {
        /// <summary>
        /// Login
        /// </summary>
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
