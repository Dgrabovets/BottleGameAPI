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
    /// Сервис баланса приложения
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="AppBalanceService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    public class AppBalanceService(ApplicationDbContext context, ILogger<AppBalanceService> logger) : IAppBalanceService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<AppBalanceService> _logger = logger;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[AppBalanceService]";

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
    }
}
