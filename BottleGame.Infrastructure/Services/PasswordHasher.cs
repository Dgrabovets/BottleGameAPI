using BottleGame.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// <inheritdoc cref="IPasswordHasher"/>
    /// </summary>
    public class PasswordHasher(IConfiguration config) : IPasswordHasher
    {
        #region Fields

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
            // ���������� ��������� ����
            var saltBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            string salt = Convert.ToBase64String(saltBytes);

            // �������� ������ � �����
            string hash = Hash(password + salt);
            return (hash, salt);
        }

        /// <summary>
        /// <inheritdoc cref="IPasswordHasher.Hash(string)"/>
        /// </summary>
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
            string computedHash = Hash(providedPassword + salt);
            return hashedPassword == computedHash;
        }

        #endregion Methods
    }
}


