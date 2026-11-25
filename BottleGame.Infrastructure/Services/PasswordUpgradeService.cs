using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// <inheritdoc cref="IPasswordUpgradeService"/>
    /// </summary>
    public class PasswordUpgradeService(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<PasswordUpgradeService> logger) : IPasswordUpgradeService
    {
        #region Fields

        /// <inheritdoc cref="ApplicationDbContext"/>
        private readonly ApplicationDbContext _context = context;

        /// <inheritdoc cref="IPasswordHasher"/>
        private readonly IPasswordHasher _passwordHasher = passwordHasher;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<PasswordUpgradeService> _logger = logger;

        /// <summary>
        /// Prefix of the log messages
        /// </summary>
        private const string Prefix = "[Password Upgrade]";

        #endregion Fields

        #region Methods

        /// <summary>
        /// <inheritdoc cref="IPasswordUpgradeService.UpgradeIfNeededAsync"/>
        /// </summary>
        public async Task UpgradeIfNeededAsync(AdministrationRoleUser admin, string password)
        {
            if (!_passwordHasher.NeedsRehash(admin.PasswordHash))
            {
                return;
            }

            try
            {
                var (hash, salt) = _passwordHasher.HashWithSalt(password);
                admin.PasswordHash = hash;
                admin.Salt = salt;
                await _context.SaveChangesAsync();
                _logger.LogInformation("{Prefix} Password hash of {Login} upgraded", Prefix, admin.Login);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Prefix} Could not upgrade the password hash of {Login}", Prefix, admin.Login);
            }
        }

        #endregion Methods
    }
}
