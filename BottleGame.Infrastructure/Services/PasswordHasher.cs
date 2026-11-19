using BottleGame.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// <inheritdoc cref="IPasswordHasher"/>
    /// </summary>
    /// <remarks>
    /// New hashes are PBKDF2-HMAC-SHA256 with a random salt, written as <c>v2$iterations$hash</c>.
    /// The password is first mixed with the server secret, so a stolen database alone is not enough to test guesses.
    /// Hashes made by the first version (one HMAC-SHA256 round, no prefix) still verify and are replaced on the next sign-in
    /// </remarks>
    public class PasswordHasher(IConfiguration config) : IPasswordHasher
    {
        #region Fields

        /// <summary>
        /// Marker of the current hash format
        /// </summary>
        private const string CurrentPrefix = "v2";

        /// <summary>
        /// Number of PBKDF2 rounds for new hashes
        /// </summary>
        private const int Iterations = 600_000;

        /// <summary>
        /// Smallest and largest number of rounds accepted when a stored hash is checked,
        /// so a damaged row cannot make a sign-in run for minutes
        /// </summary>
        private const int MinIterations = 100_000, MaxIterations = 5_000_000;

        /// <summary>
        /// Length of the derived key in bytes
        /// </summary>
        private const int KeySize = 32;

        /// <summary>
        /// Secret key mixed into every password hash
        /// </summary>
        private readonly byte[] _secretKey = Encoding.UTF8.GetBytes(config["Security:PasswordHashSecretKey"]!);

        #endregion Fields

        #region Methods

        /// <summary>
        /// <inheritdoc cref="IPasswordHasher.HashWithSalt(string)"/>
        /// </summary>
        public (string Hash, string Salt) HashWithSalt(string password)
        {
            // Generate a random salt
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var hash = Derive(password, saltBytes, Iterations);

            return ($"{CurrentPrefix}${Iterations}${Convert.ToBase64String(hash)}", Convert.ToBase64String(saltBytes));
        }

        /// <summary>
        /// <inheritdoc cref="IPasswordHasher.Hash(string)"/>
        /// </summary>
        /// <remarks>
        /// The first version of the hash, kept to check old rows. Do not use it for new passwords
        /// </remarks>
        public string Hash(string input)
        {
            using var hmac = new HMACSHA256(_secretKey);
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = hmac.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// <inheritdoc cref="IPasswordHasher.Verify(string, string, string)"/>
        /// </summary>
        public bool Verify(string hashedPassword, string providedPassword, string salt)
        {
            if (!hashedPassword.StartsWith(CurrentPrefix + "$", StringComparison.Ordinal))
            {
                // Hash of the first version
                var legacy = Hash(providedPassword + salt);
                return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hashedPassword), Encoding.UTF8.GetBytes(legacy));
            }

            var parts = hashedPassword.Split('$');
            if (parts.Length != 3
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
                || iterations is < MinIterations or > MaxIterations)
            {
                return false;
            }

            try
            {
                var expected = Convert.FromBase64String(parts[2]);
                var computed = Derive(providedPassword, Convert.FromBase64String(salt), iterations);
                return CryptographicOperations.FixedTimeEquals(expected, computed);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// <inheritdoc cref="IPasswordHasher.NeedsRehash(string)"/>
        /// </summary>
        public bool NeedsRehash(string hashedPassword)
        {
            var parts = hashedPassword.Split('$');
            return parts.Length != 3
                || parts[0] != CurrentPrefix
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
                || iterations < Iterations;
        }

        /// <summary>
        /// Derives the key of a password: the password is mixed with the server secret, then stretched with PBKDF2
        /// </summary>
        /// <param name="password">Password</param>
        /// <param name="salt">Random salt of the account</param>
        /// <param name="iterations">Number of PBKDF2 rounds</param>
        /// <returns>The derived key</returns>
        private byte[] Derive(string password, byte[] salt, int iterations)
        {
            var mixed = HMACSHA256.HashData(_secretKey, Encoding.UTF8.GetBytes(password));
            return Rfc2898DeriveBytes.Pbkdf2(mixed, salt, iterations, HashAlgorithmName.SHA256, KeySize);
        }

        #endregion Methods
    }
}
