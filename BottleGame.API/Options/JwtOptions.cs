using System.ComponentModel.DataAnnotations;

namespace BottleGame.API.Options
{
    /// <summary>
    /// JWT authentication settings
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// Secret key used to sign tokens (at least 32 characters).
        /// Set through the configuration or the JWT__Key environment variable
        /// </summary>
        [Required(ErrorMessage = "JWT:Key is required. Set the JWT__Key environment variable or the JWT_KEY secret in GitHub Actions.")]
        [MinLength(32, ErrorMessage = "JWT:Key must be at least 32 characters long.")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Token issuer
        /// </summary>
        public string Issuer { get; set; } = "BottleAppAPI";

        /// <summary>
        /// Token audience
        /// </summary>
        public string Audience { get; set; } = "BottleGame";

        /// <summary>
        /// Token lifetime in minutes
        /// </summary>
        [Range(1, 10080)]
        public int ExpireMinutes { get; set; } = 360;
    }
}
