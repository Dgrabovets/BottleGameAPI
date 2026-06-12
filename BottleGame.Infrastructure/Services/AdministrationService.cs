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
        public async Task<Result<GetAppGeneralStatisticsResponse>> GetStatistics(GetAppGeneralStatisticsRequest? request = null)
        {
            var hasPeriod = request?.DateFrom.HasValue == true && request.DateTill.HasValue;

            DateTime? rangeFrom = null;
            DateTime? rangeTill = null;

            if (hasPeriod)
            {
                var validation = ValidateDateRange(request!.DateFrom!.Value, request.DateTill!.Value);
                if (validation is not null)
                {
                    return Result<GetAppGeneralStatisticsResponse>.Failure(validation);
                }

                (rangeFrom, rangeTill) = NormalizeInclusiveUtcRange(request!.DateFrom!.Value, request.DateTill!.Value);
            }
            else if (request?.DateFrom.HasValue == true || request?.DateTill.HasValue == true)
            {
                return Result<GetAppGeneralStatisticsResponse>.Failure("Укажите обе даты периода: dateFrom и dateTill");
            }

            var bannedPlayerIds = _context.ActivelyBannedPlayerIds();

            var usersQuery = _context.Players.AsNoTracking().WhereNotActivelyBanned(bannedPlayerIds);
            if (rangeFrom.HasValue && rangeTill.HasValue)
            {
                usersQuery = usersQuery.Where(p =>
                    p.RegisteredInAppAt >= rangeFrom.Value
                    && p.RegisteredInAppAt <= rangeTill.Value);
            }

            var usersTotal = await usersQuery.CountAsync();

            var depositsQuery = FilterSuccessfullyProcessedTransactions(
                _context.MoneyTransactions.AsNoTracking().WherePlayerNotActivelyBanned(bannedPlayerIds),
                TransactionTypes.Deposit);

            if (rangeFrom.HasValue && rangeTill.HasValue)
            {
                depositsQuery = FilterTransactionsByProcessedDate(depositsQuery,
                                                                  rangeFrom.Value,
                                                                  rangeTill.Value);
            }

            var depositsTotal = await depositsQuery.SumAsync(t => t.Amount);

            var withdrawalsQuery = FilterSuccessfullyProcessedTransactions(
                _context.MoneyTransactions.AsNoTracking().WherePlayerNotActivelyBanned(bannedPlayerIds),
                TransactionTypes.Withdrawal);

            if (rangeFrom.HasValue && rangeTill.HasValue)
            {
                withdrawalsQuery = FilterTransactionsByProcessedDate(withdrawalsQuery,
                                                                     rangeFrom.Value,
                                                                     rangeTill.Value);
            }

            var withdrawalsTotal = await withdrawalsQuery.SumAsync(t => t.Amount);

            decimal? balancesTotal = null;
            if (!hasPeriod)
            {
                balancesTotal = await _context.PlayersBalances
                    .WherePlayerNotActivelyBanned(bannedPlayerIds)
                    .SumAsync(b => b.Amount);
            }

            var pendingWithdrawalsTotal = await _context.MoneyTransactions
                .AsNoTracking()
                .WherePlayerNotActivelyBanned(bannedPlayerIds)
                .Where(t => t.TransactionTypeId == TransactionTypes.Withdrawal
                         && t.TransactionStatusId == TransactionStatuses.Pending)
                .SumAsync(t => t.Amount);

            var response = new GetAppGeneralStatisticsResponse
            {
                UsersTotal = usersTotal,
                DepositsTotal = depositsTotal,
                WithdrawalsTotal = withdrawalsTotal,
                IncomeTotal = depositsTotal - withdrawalsTotal,
                BalancesTotal = balancesTotal,
                PendingWithdrawalsTotal = pendingWithdrawalsTotal
            };

            _logger.LogInformation("{Prefix} Успешно получена админская статистика", Prefix);

            return Result<GetAppGeneralStatisticsResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<GetAppStatisticsTimelineResponse>> GetStatisticsTimeline(
            GetAppStatisticsTimelineRequest request)
        {
            var validation = ValidateDateRange(request.DateFrom, request.DateTill);
            if (validation is not null)
            {
                return Result<GetAppStatisticsTimelineResponse>.Failure(validation);
            }

            var (rangeFrom, rangeTill) = NormalizeInclusiveUtcRange(request.DateFrom, request.DateTill);
            var bucket = SelectTimelineBucket(rangeFrom, rangeTill);
            var bucketStarts = BuildBucketStarts(rangeFrom, rangeTill, bucket);

            var bannedPlayerIds = _context.ActivelyBannedPlayerIds();

            var registrationDates = await _context.Players
                .AsNoTracking()
                .WhereNotActivelyBanned(bannedPlayerIds)
                .Select(p => p.RegisteredInAppAt)
                .ToListAsync();

            var transactions = await _context.MoneyTransactions
                .AsNoTracking()
                .WherePlayerNotActivelyBanned(bannedPlayerIds)
                .Where(t => t.TransactionStatusId == TransactionStatuses.Approved
                         && t.IsProcessingSuccess == true
                         && t.ProcessedAt != null
                         && (t.TransactionTypeId == TransactionTypes.Deposit
                             || t.TransactionTypeId == TransactionTypes.Withdrawal))
                .Where(t => t.ProcessedAt >= rangeFrom && t.ProcessedAt <= rangeTill)
                .Select(t => new TransactionTimelineItem(
                    t.TransactionTypeId,
                    t.Amount,
                    t.ProcessedAt!.Value))
                .ToListAsync();

            var points = new List<StatisticsTimelinePoint>(bucketStarts.Count);

            foreach (var bucketStart in bucketStarts)
            {
                var bucketEnd = GetBucketEnd(bucketStart, bucket, rangeTill);

                var depositsAmount = transactions
                    .Where(t => t.TypeId == TransactionTypes.Deposit
                             && t.EffectiveAt >= bucketStart
                             && t.EffectiveAt <= bucketEnd)
                    .Sum(t => t.Amount);

                var withdrawalsAmount = transactions
                    .Where(t => t.TypeId == TransactionTypes.Withdrawal
                             && t.EffectiveAt >= bucketStart
                             && t.EffectiveAt <= bucketEnd)
                    .Sum(t => t.Amount);

                points.Add(new StatisticsTimelinePoint
                {
                    Date = bucketStart,
                    UsersCount = registrationDates.Count(d => d <= bucketEnd),
                    DepositsAmount = depositsAmount,
                    WithdrawalsAmount = withdrawalsAmount,
                    IncomeAmount = depositsAmount - withdrawalsAmount
                });
            }

            var response = new GetAppStatisticsTimelineResponse
            {
                DateFrom = rangeFrom,
                DateTill = rangeTill,
                Bucket = bucket,
                Points = points
            };

            _logger.LogInformation("{Prefix} Успешно получена временная шкала статистики", Prefix);

            return Result<GetAppStatisticsTimelineResponse>.Success(response);
        }

        #endregion Statistics Methods

        #region Private Statistics Methods

        /// <summary>
        /// Максимальная длина запрашиваемого периода статистики в днях (100 лет)
        /// </summary>
        private const int MaxStatisticsRangeDays = 36_500;

        /// <summary>
        /// Проверяет корректность диапазона дат для статистики
        /// </summary>
        /// <param name="dateFrom">Дата начала периода</param>
        /// <param name="dateTill">Дата окончания периода</param>
        /// <returns>Сообщение об ошибке или <c>null</c>, если диапазон допустим</returns>
        private static string? ValidateDateRange(DateTime dateFrom, DateTime dateTill)
        {
            var from = dateFrom.Date;
            var till = dateTill.Date;

            if (from > till)
            {
                return "Дата начала периода не может быть позже даты окончания";
            }

            var days = (till - from).Days + 1;
            if (days > MaxStatisticsRangeDays)
            {
                return $"Максимальный период — {MaxStatisticsRangeDays} дней";
            }

            return null;
        }

        /// <summary>
        /// Нормализует границы периода к включительному UTC-диапазону
        /// </summary>
        /// <param name="dateFrom">Дата начала периода</param>
        /// <param name="dateTill">Дата окончания периода</param>
        /// <returns>Начало и конец периода в UTC</returns>
        private static (DateTime From, DateTime Till) NormalizeInclusiveUtcRange(
            DateTime dateFrom,
            DateTime dateTill)
        {
            var from = DateTime.SpecifyKind(dateFrom.Date, DateTimeKind.Utc);
            var till = DateTime.SpecifyKind(dateTill.Date, DateTimeKind.Utc)
                .AddDays(1)
                .AddTicks(-1);

            return (from, till);
        }

        /// <summary>
        /// Фильтрует успешно обработанные транзакции указанного типа
        /// </summary>
        /// <param name="query">Исходный запрос транзакций</param>
        /// <param name="transactionTypeId">Идентификатор типа транзакции</param>
        /// <returns>Запрос с фильтром по одобренным и успешно завершённым операциям</returns>
        private static IQueryable<MoneyTransaction> FilterSuccessfullyProcessedTransactions(
            IQueryable<MoneyTransaction> query,
            int transactionTypeId)
        {
            return query.Where(t =>
                t.TransactionTypeId == transactionTypeId
                && t.TransactionStatusId == TransactionStatuses.Approved
                && t.IsProcessingSuccess == true
                && t.ProcessedAt != null);
        }

        /// <summary>
        /// Ограничивает транзакции датой успешной обработки в заданном периоде
        /// </summary>
        /// <param name="query">Исходный запрос транзакций</param>
        /// <param name="rangeFrom">Начало периода (включительно)</param>
        /// <param name="rangeTill">Конец периода (включительно)</param>
        /// <returns>Запрос с фильтром по <see cref="MoneyTransaction.ProcessedAt"/></returns>
        private static IQueryable<MoneyTransaction> FilterTransactionsByProcessedDate(
            IQueryable<MoneyTransaction> query,
            DateTime rangeFrom,
            DateTime rangeTill)
        {
            return query.Where(t =>
                t.ProcessedAt != null
                && t.ProcessedAt >= rangeFrom
                && t.ProcessedAt <= rangeTill);
        }

        /// <summary>
        /// Выбирает размер бакета временной шкалы в зависимости от длины периода
        /// </summary>
        /// <param name="rangeFrom">Начало периода</param>
        /// <param name="rangeTill">Конец периода</param>
        /// <returns><c>day</c>, <c>week</c> или <c>month</c></returns>
        private static string SelectTimelineBucket(DateTime rangeFrom, DateTime rangeTill)
        {
            var days = (rangeTill.Date - rangeFrom.Date).Days + 1;
            if (days <= 90)
            {
                return "day";
            }

            if (days <= 366)
            {
                return "week";
            }

            if (days <= 1_825)
            {
                return "month";
            }

            return "year";
        }

        /// <summary>
        /// Формирует список дат начала бакетов для временной шкалы
        /// </summary>
        /// <param name="rangeFrom">Начало периода</param>
        /// <param name="rangeTill">Конец периода</param>
        /// <param name="bucket">Размер бакета: <c>day</c>, <c>week</c> или <c>month</c></param>
        /// <returns>Список UTC-дат начала бакетов</returns>
        private static List<DateTime> BuildBucketStarts(
            DateTime rangeFrom,
            DateTime rangeTill,
            string bucket)
        {
            var starts = new List<DateTime>();
            var current = rangeFrom.Date;

            while (current <= rangeTill.Date)
            {
                starts.Add(DateTime.SpecifyKind(current, DateTimeKind.Utc));
                current = bucket switch
                {
                    "week" => current.AddDays(7),
                    "month" => current.AddMonths(1),
                    "year" => current.AddYears(1),
                    _ => current.AddDays(1)
                };
            }

            return starts;
        }

        /// <summary>
        /// Возвращает конец бакета с учётом границы запрошенного периода
        /// </summary>
        /// <param name="bucketStart">Дата начала бакета</param>
        /// <param name="bucket">Размер бакета: <c>day</c>, <c>week</c> или <c>month</c></param>
        /// <param name="rangeTill">Конец запрошенного периода</param>
        /// <returns>Дата и время конца бакета</returns>
        private static DateTime GetBucketEnd(DateTime bucketStart, string bucket, DateTime rangeTill)
        {
            var end = bucket switch
            {
                "week" => bucketStart.AddDays(7).AddTicks(-1),
                "month" => bucketStart.AddMonths(1).AddTicks(-1),
                "year" => bucketStart.AddYears(1).AddTicks(-1),
                _ => bucketStart.AddDays(1).AddTicks(-1)
            };

            return end > rangeTill ? rangeTill : end;
        }

        /// <summary>
        /// Упрощённое представление транзакции для агрегации временной шкалы
        /// </summary>
        /// <param name="TypeId">Идентификатор типа транзакции</param>
        /// <param name="Amount">Сумма транзакции</param>
        /// <param name="EffectiveAt">Дата успешной обработки</param>
        private sealed record TransactionTimelineItem(
            int TypeId,
            decimal Amount,
            DateTime EffectiveAt);

        #endregion Private Statistics Methods

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




