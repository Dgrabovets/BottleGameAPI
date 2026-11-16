namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Service for hashing passwords
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Creates a salt and hashes the password with it
        /// </summary>
        (string Hash, string Salt) HashWithSalt(string password);

        /// <summary>
        /// Hashes a string
        /// </summary>
        string Hash(string input);

        /// <summary>
        /// Verifies a password
        /// </summary>
        bool Verify(string hashedPassword, string providedPassword, string salt);
    }
}

