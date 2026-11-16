namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Authorization response
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// Player id
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// JWT access token
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}
