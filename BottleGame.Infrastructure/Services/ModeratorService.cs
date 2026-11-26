using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Moderator management service
    /// </summary>
    public class ModeratorService : IModeratorService
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<ModeratorService> _logger;

        /// <summary>
        /// Password hashing service
        /// </summary>
        private readonly IPasswordHasher _passwordHasher;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string PREFIX = "[ModeratorService]";

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="ModeratorService"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger</param>
        /// <param name="passwordHasher">Password hashing service</param>
        public ModeratorService(ApplicationDbContext context, ILogger<ModeratorService> logger, IPasswordHasher passwordHasher)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        #region Moderator Methods

        /// <inheritdoc/>
        public async Task<Result> RegisterModerator(AdministrationRoleUserCredentials request)
        {
            if (await _context.AdminCreds.AnyAsync(a => a.Login == request.Login))
            {
                _logger.LogWarning("{Prefix} Moderator already exists", PREFIX);
                return Result.Conflict("Moderator already exists");
            }

            var (hash, salt) = _passwordHasher.HashWithSalt(request.Password);

            var moderator = new AdministrationRoleUser
            {
                Login = request.Login,
                PasswordHash = hash,
                Salt = salt,
                CreatedAt = DateTime.UtcNow,
                Role = AdministrationRoles.MODERATOR
            };

            _context.AdminCreds.Add(moderator);
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} New moderator registered: {Login}", PREFIX, LogSanitizer.Clean(request.Login));

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<object>> GetModerators(int? moderatorId)
        {
            if (moderatorId.HasValue)
            {
                var moderator = await _context.AdminCreds
                    .Where(a => a.Role == AdministrationRoles.MODERATOR && a.Id == moderatorId.Value)
                    .Select(a => new ModeratorDTO
                    {
                        Id = a.Id,
                        Login = a.Login,
                        CreatedAt = a.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (moderator is null)
                {
                    _logger.LogWarning("{Prefix} Moderator with ID {Id} not found", PREFIX, moderatorId.Value);
                    return Result<object>.NotFound("Moderator not found");
                }

                return Result<object>.Success(moderator);
            }

            var moderators = await _context.AdminCreds
                .Where(a => a.Role == AdministrationRoles.MODERATOR)
                .Select(a => new ModeratorDTO
                {
                    Id = a.Id,
                    Login = a.Login,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Result<object>.Success(moderators);
        }

        /// <inheritdoc/>
        public async Task<Result> DeleteModerator(int moderatorId)
        {
            var moderator = await _context.AdminCreds
                .FirstOrDefaultAsync(a => a.Id == moderatorId && a.Role == AdministrationRoles.MODERATOR);

            if (moderator is null)
            {
                _logger.LogWarning("{Prefix} Moderator with ID {Id} not found", PREFIX, moderatorId);
                return Result.NotFound("Moderator not found");
            }

            _context.AdminCreds.Remove(moderator);
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Moderator with ID {Id} deleted", PREFIX, moderatorId);

            return Result.Success();
        }

        #endregion Moderator Methods
    }
}
