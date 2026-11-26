using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// <inheritdoc cref="IPasswordUpgradeService"/>
    /// </summary>
    public class PasswordUpgradeService : IPasswordUpgradeService
    {
        #region Fields

        /// <inheritdoc cref="ApplicationDbContext"/>
        private readonly ApplicationDbContext _context;

        /// <inheritdoc cref="IPasswordHasher"/>
        private readonly IPasswordHasher _passwordHasher;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<PasswordUpgradeService> _logger;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="PasswordUpgradeService"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="passwordHasher">Password hashing service</param>
        /// <param name="logger">Logger</param>
        public PasswordUpgradeService(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<PasswordUpgradeService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

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
                _logger.LogInformation("Password hash of {Login} upgraded", admin.Login);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not upgrade the password hash of {Login}", admin.Login);
            }
        }

        #endregion Methods
    }
}
