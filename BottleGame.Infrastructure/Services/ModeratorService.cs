using Microsoft.Extensions.Logging;
using BottleGame.Application.Common;
using BottleGame.Domain.Entities;
using BottleGame.Domain.Constants;
using BottleGame.Infrastructure.Data;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using Microsoft.EntityFrameworkCore;
using BottleGame.Application.Interfaces;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис управления модераторами
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="ModeratorService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    /// <param name="passwordHasher">Сервис хеширования паролей</param>
    public class ModeratorService(ApplicationDbContext context, ILogger<ModeratorService> logger, IPasswordHasher passwordHasher) : IModeratorService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<ModeratorService> _logger = logger;

        /// <summary>
        /// Сервис хеширования паролей
        /// </summary>
        private readonly IPasswordHasher _passwordHasher = passwordHasher;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[ModeratorService]";

        #endregion Fields

        #region Moderator Methods

        /// <inheritdoc/>
        public async Task<Result> RegisterModerator(AdministrationRoleUserCredentials request)
        {
            if (await _context.AdminCreds.AnyAsync(a => a.Login == request.Login))
            {
                _logger.LogWarning("{Prefix} Модератор уже есть", Prefix);
                return Result.Conflict("Модератор уже существует");
            }

            var (hash, salt) = _passwordHasher.HashWithSalt(request.Password);

            var moderator = new AdministrationRoleUser
            {
                Login = request.Login,
                PasswordHash = hash,
                Salt = salt,
                CreatedAt = DateTime.UtcNow,
                Role = AdministrationRoles.Moderator
            };

            _context.AdminCreds.Add(moderator);
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Зарегистрирован новый модератор: {Login}", Prefix, request.Login);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<object>> GetModerators(int? moderatorId)
        {
            if (moderatorId.HasValue)
            {
                var moderator = await _context.AdminCreds
                    .Where(a => a.Role == AdministrationRoles.Moderator && a.Id == moderatorId.Value)
                    .Select(a => new ModeratorDTO
                    {
                        Id = a.Id,
                        Login = a.Login,
                        CreatedAt = a.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (moderator is null)
                {
                    _logger.LogWarning("{Prefix} Модератор с ID {Id} не найден", Prefix, moderatorId.Value);
                    return Result<object>.NotFound("Модератор не найден");
                }

                return Result<object>.Success(moderator);
            }

            var moderators = await _context.AdminCreds
                .Where(a => a.Role == AdministrationRoles.Moderator)
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
                .FirstOrDefaultAsync(a => a.Id == moderatorId && a.Role == AdministrationRoles.Moderator);

            if (moderator is null)
            {
                _logger.LogWarning("{Prefix} Модератор с ID {Id} не найден", Prefix, moderatorId);
                return Result.NotFound("Модератор не найден");
            }

            _context.AdminCreds.Remove(moderator);
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Модератор с ID {Id} удален", Prefix, moderatorId);

            return Result.Success();
        }

        #endregion Moderator Methods
    }
}
