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
    /// Application statistics service
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<StatisticsService> _logger;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="StatisticsService"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger</param>
        public StatisticsService(ApplicationDbContext context, ILogger<StatisticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

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
                return Result<GetAppGeneralStatisticsResponse>.Failure("Specify both dates of the period: dateFrom and dateTill");
            }

            var bannedPlayerIds = await _context.GetActivelyBannedPlayerIdsAsync();

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
                TransactionTypes.DEPOSIT);

            if (rangeFrom.HasValue && rangeTill.HasValue)
            {
                depositsQuery = FilterTransactionsByProcessedDate(depositsQuery,
                                                                  rangeFrom.Value,
                                                                  rangeTill.Value);
            }

            var depositsTotal = await depositsQuery.SumAsync(t => t.Amount);

            var withdrawalsQuery = FilterSuccessfullyProcessedTransactions(
                _context.MoneyTransactions.AsNoTracking().WherePlayerNotActivelyBanned(bannedPlayerIds),
                TransactionTypes.WITHDRAWAL);

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
                .Where(t => t.TransactionTypeId == TransactionTypes.WITHDRAWAL
                         && t.TransactionStatusId == TransactionStatuses.PENDING)
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

            _logger.LogInformation("Administrator statistics retrieved successfully");

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

            var bannedPlayerIds = await _context.GetActivelyBannedPlayerIdsAsync();

            var registrationDates = await _context.Players
                .AsNoTracking()
                .WhereNotActivelyBanned(bannedPlayerIds)
                .Select(p => p.RegisteredInAppAt)
                .ToListAsync();

            var transactions = await _context.MoneyTransactions
                .AsNoTracking()
                .WherePlayerNotActivelyBanned(bannedPlayerIds)
                .Where(t => t.TransactionStatusId == TransactionStatuses.APPROVED
                         && t.IsProcessingSuccess == true
                         && t.ProcessedAt != null
                         && (t.TransactionTypeId == TransactionTypes.DEPOSIT
                             || t.TransactionTypeId == TransactionTypes.WITHDRAWAL))
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
                    .Where(t => t.TypeId == TransactionTypes.DEPOSIT
                             && t.EffectiveAt >= bucketStart
                             && t.EffectiveAt <= bucketEnd)
                    .Sum(t => t.Amount);

                var withdrawalsAmount = transactions
                    .Where(t => t.TypeId == TransactionTypes.WITHDRAWAL
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

            _logger.LogInformation("Statistics timeline retrieved successfully");

            return Result<GetAppStatisticsTimelineResponse>.Success(response);
        }

        #endregion Statistics Methods

        #region Private Statistics Methods

        /// <summary>
        /// Maximum length of the requested statistics period in days (100 years)
        /// </summary>
        private const int MAX_STATISTICS_RANGE_DAYS = 36_500;

        /// <summary>
        /// Checks that the date range for the statistics is valid
        /// </summary>
        /// <param name="dateFrom">Start date of the period</param>
        /// <param name="dateTill">End date of the period</param>
        /// <returns>Error message or <c>null</c> if the range is valid</returns>
        private static string? ValidateDateRange(DateTime dateFrom, DateTime dateTill)
        {
            var from = dateFrom.Date;
            var till = dateTill.Date;

            if (from > till)
            {
                return "The start date of the period cannot be later than the end date";
            }

            var days = (till - from).Days + 1;
            if (days > MAX_STATISTICS_RANGE_DAYS)
            {
                return $"The maximum period is {MAX_STATISTICS_RANGE_DAYS} days";
            }

            return null;
        }

        /// <summary>
        /// Normalizes the period boundaries to an inclusive UTC range
        /// </summary>
        /// <param name="dateFrom">Start date of the period</param>
        /// <param name="dateTill">End date of the period</param>
        /// <returns>Start and end of the period in UTC</returns>
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
        /// Filters successfully processed transactions of the given type
        /// </summary>
        /// <param name="query">Source transaction query</param>
        /// <param name="transactionTypeId">Transaction type identifier</param>
        /// <returns>Query filtered to approved and successfully completed operations</returns>
        private static IQueryable<MoneyTransaction> FilterSuccessfullyProcessedTransactions(
            IQueryable<MoneyTransaction> query,
            int transactionTypeId)
        {
            return query.Where(t =>
                t.TransactionTypeId == transactionTypeId
                && t.TransactionStatusId == TransactionStatuses.APPROVED
                && t.IsProcessingSuccess == true
                && t.ProcessedAt != null);
        }

        /// <summary>
        /// Restricts transactions by the date of successful processing within the given period
        /// </summary>
        /// <param name="query">Source transaction query</param>
        /// <param name="rangeFrom">Start of the period (inclusive)</param>
        /// <param name="rangeTill">End of the period (inclusive)</param>
        /// <returns>Query filtered by <see cref="MoneyTransaction.ProcessedAt"/></returns>
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
        /// Chooses the size of the timeline bucket depending on the length of the period
        /// </summary>
        /// <param name="rangeFrom">Start of the period</param>
        /// <param name="rangeTill">End of the period</param>
        /// <returns><c>day</c>, <c>week</c> or <c>month</c></returns>
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
        /// Builds the list of bucket start dates for the timeline
        /// </summary>
        /// <param name="rangeFrom">Start of the period</param>
        /// <param name="rangeTill">End of the period</param>
        /// <param name="bucket">Bucket size: <c>day</c>, <c>week</c> or <c>month</c></param>
        /// <returns>List of UTC bucket start dates</returns>
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
        /// Returns the end of a bucket, taking the boundary of the requested period into account
        /// </summary>
        /// <param name="bucketStart">Bucket start date</param>
        /// <param name="bucket">Bucket size: <c>day</c>, <c>week</c> or <c>month</c></param>
        /// <param name="rangeTill">End of the requested period</param>
        /// <returns>Date and time of the bucket end</returns>
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

        #endregion Private Statistics Methods
    }
}
