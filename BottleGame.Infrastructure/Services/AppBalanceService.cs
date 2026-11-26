using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Application balance service
    /// </summary>
    public class AppBalanceService : IAppBalanceService
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<AppBalanceService> _logger;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[AppBalanceService]";

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="AppBalanceService"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger</param>
        public AppBalanceService(ApplicationDbContext context, ILogger<AppBalanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region App Balance Methods

        /// <inheritdoc/>
        public async Task<Result<AppBalanceDTO>> GetAppBalance()
        {
            var balance = await _context.AppBalance.FirstOrDefaultAsync();

            if (balance is null)
            {
                _logger.LogWarning("{Prefix} Application balance record not found", Prefix);
                return Result<AppBalanceDTO>.NotFound("Application balance is not initialized");
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
                _logger.LogWarning("{Prefix} No data found to update, the data integrity is probably broken", Prefix);
                return Result.Failure("No data found to update, the data integrity is probably broken");
            }

            appBalance.LowerThreshold = request.LowerThreshold;
            appBalance.LowerThresholdUpdatedAt = DateTime.UtcNow;
            appBalance.LowerThresholdAdminIdUpdatedBy = adminId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Lower threshold of the application balance updated: {LowerThreshold}", Prefix, request.LowerThreshold);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<decimal>> TopUpAppBalance(TopUpAppBalanceRequest request, int adminId)
        {
            if (request.Amount <= 0)
            {
                _logger.LogWarning("{Prefix} Top-up amount must be positive: {Amount}", Prefix, request.Amount);
                return Result<decimal>.Failure("Top-up amount must be positive");
            }

            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();

            if (appBalance is null)
            {
                _logger.LogWarning("{Prefix} Application balance data not found", Prefix);
                return Result<decimal>.Failure("Application balance data not found");
            }

            appBalance.Amount += request.Amount;
            appBalance.BalanceAmountUpdatedAt = DateTime.UtcNow;
            appBalance.BalanceAmountAdminIdUpdatedBy = adminId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Application balance topped up by {Amount}. New balance: {NewBalance}",
                Prefix, request.Amount, appBalance.Amount);

            return Result<decimal>.Success(appBalance.Amount);
        }

        #endregion App Balance Methods
    }
}
