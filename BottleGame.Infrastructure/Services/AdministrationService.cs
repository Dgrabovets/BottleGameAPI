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
    /// Сервис администрирования
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="AdministrationService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    /// <param name="passwordHasher">Сервис хеширования паролей</param>
    public class AdministrationService(ApplicationDbContext context, ILogger<AdministrationService> logger, IPasswordHasher passwordHasher) : IAdministrationService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<AdministrationService> _logger = logger;

        /// <summary>
        /// Сервис хеширования паролей
        /// </summary>
        private readonly IPasswordHasher _passwordHasher = passwordHasher;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[AdministrationService]";

        #endregion Fields

        #region App Balance Methods

        /// <inheritdoc/>
        public async Task<Result<AppBalanceDTO>> GetAppBalance()
        {
            var balance = await _context.AppBalance.FirstOrDefaultAsync();

            if (balance is null)
            {
                _logger.LogWarning("{Prefix} Запись баланса приложения не найдена", Prefix);
                return Result<AppBalanceDTO>.NotFound("Баланс приложения не инициализирован");
            }

            var admins = await _context.AdminCreds.ToDictionaryAsync(m => m.Id, m => m.Login);

            string GetAdminLogin(int? adminId) =>
                adminId.HasValue && admins.TryGetValue(adminId.Value, out var login)
                    ? login
                    : null!;

            var result = new AppBalanceDTO
            {
                Amount = balance.Amount,
                BalanceAmountUpdatedAt = balance.BalanceAmountUpdatedAt,
                BalanceAmountAdminIdUpdatedBy = balance.BalanceAmountAdminIdUpdatedBy,
                BalanceAmountAdminLoginUpdatedBy = GetAdminLogin(balance.BalanceAmountAdminIdUpdatedBy),
                LowerThreshold = balance.LowerThreshold,
                LowerThresholdUpdatedAt = balance.LowerThresholdUpdatedAt,
                LowerThresholdAdminIdUpdatedBy = balance.LowerThresholdAdminIdUpdatedBy,
                LowerThresholdAdminLoginUpdatedBy = GetAdminLogin(balance.LowerThresholdAdminIdUpdatedBy)
            };

            return Result<AppBalanceDTO>.Success(result);
        }

        /// <inheritdoc/>
        public async Task<Result> UpdateAppBalanceThreshold(UpdateAppBalanceThresholdRequest request, int adminId)
        {
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();

            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Не найдены данные для обновления, скорее всего нарушена целостность данных", Prefix);
                return Result.Failure("Не найдены данные для обновления, скорее всего нарушена целостность данных");
            }

            appBalance.LowerThreshold = request.LowerThreshold;
            appBalance.LowerThresholdUpdatedAt = DateTime.UtcNow;
            appBalance.LowerThresholdAdminIdUpdatedBy = adminId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Обновлен нижний порог для баланса приложения: {LowerThreshold}", Prefix, request.LowerThreshold);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<decimal>> TopUpAppBalance(TopUpAppBalanceRequest request, int adminId)
        {
            if (request.Amount <= 0)
            {
                _logger.LogWarning("{Prefix} Сумма пополнения должна быть положительной: {Amount}", Prefix, request.Amount);
                return Result<decimal>.Failure("Сумма пополнения должна быть положительной");
            }

            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();

            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Не найдены данные баланса приложения", Prefix);
                return Result<decimal>.Failure("Не найдены данные баланса приложения");
            }

            appBalance.Amount += request.Amount;
            appBalance.BalanceAmountUpdatedAt = DateTime.UtcNow;
            appBalance.BalanceAmountAdminIdUpdatedBy = adminId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Баланс приложения пополнен на {Amount}. Новый баланс: {NewBalance}",
                Prefix, request.Amount, appBalance.Amount);

            return Result<decimal>.Success(appBalance.Amount);
        }

        #endregion App Balance Methods

        #region Statistics Methods

        /// <inheritdoc/>
        public async Task<Result<GetAppGeneralStatisticsResponse>> GetStatistics()
        {
            // Используем агрегатные функции SQL вместо загрузки всех записей в память
            var usersTotal = await _context.Players.CountAsync();

            if (usersTotal == 0)
            {
                _logger.LogWarning("{Prefix} Игроки не найдены", Prefix);
                return Result<GetAppGeneralStatisticsResponse>.Success(new GetAppGeneralStatisticsResponse());
            }

            var balancesTotal = await _context.PlayersBalances
                .SumAsync(b => b.Amount);

            var depositsTotal = await _context.MoneyTransactions
                .Where(t => t.TransactionTypeId == TransactionTypes.Deposit
                         && t.TransactionStatusId == TransactionStatuses.Approved)
                .SumAsync(t => t.Amount);

            var pendingWithdrawalsTotal = await _context.MoneyTransactions
                .Where(t => t.TransactionTypeId == TransactionTypes.Withdrawal
                         && t.TransactionStatusId == TransactionStatuses.Pending)
                .SumAsync(t => t.Amount);

            var response = new GetAppGeneralStatisticsResponse
            {
                UsersTotal = usersTotal,
                DepositsTotal = depositsTotal,
                BalancesTotal = balancesTotal,
                PendingWithdrawalsTotal = pendingWithdrawalsTotal
            };

            _logger.LogInformation("{Prefix} Успешно получена админская статистика", Prefix);

            return Result<GetAppGeneralStatisticsResponse>.Success(response);
        }

        #endregion Statistics Methods

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




