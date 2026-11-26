using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Service for working with player bans
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="BanService"/>
    /// </remarks>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger</param>
    public class BanService(ApplicationDbContext context, ILogger<BanService> logger) : IBanService
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<BanService> _logger = logger;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[BanService]";

        /// <summary>
        /// Maximum number of attempts on a concurrency conflict
        /// </summary>
        private const int MaxRetries = 3;

        #endregion Fields

        #region Public Methods

        /// <inheritdoc/>
        public async Task<Result<IsBannedResponse>> IsBanned(int playerId, int? requestingPlayerId, bool isAdmin)
        {
            // Access check
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<IsBannedResponse>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result<IsBannedResponse>.NotFound($"Player with id {playerId} not found");
            }

            var banHistory = await _context.BansHistory
                .Where(b => b.PlayerId == playerId)
                .OrderByDescending(b => b.BannedAt)
                .Select(b => new BanHistoryItem
                {
                    BanReason = b.BanReason,
                    BannedAt = b.BannedAt,
                    UnbannedAt = b.UnbannedAt
                })
                .AsNoTracking()
                .ToListAsync();

            var response = new IsBannedResponse
            {
                IsBanned = player.Ban is not null && player.Ban.IsActive,
                BanReason = player.Ban?.BanReason ?? string.Empty,
                BanHistory = banHistory
            };

            _logger.LogInformation("{Prefix} Ban check for player {PlayerId}. Result: {IsBanned}",
                Prefix, playerId, response.IsBanned);

            return Result<IsBannedResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result> BanPlayer(BanPlayerRequest request)
        {
            int attempt = 0;

            while (attempt < MaxRetries)
            {
                try
                {
                    var player = await _context.Players
                        .Include(p => p.Ban)
                        .AsTracking()
                        .FirstOrDefaultAsync(p => p.Id == request.PlayerId);

                    if (player is null)
                    {
                        _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, request.PlayerId);
                        return Result.NotFound($"Player with ID {request.PlayerId} not found");
                    }

                    if (player.Ban is not null && player.Ban.IsActive)
                    {
                        _logger.LogWarning("{Prefix} Player {PlayerId} is already banned", Prefix, request.PlayerId);
                        return Result.Conflict("User already has an active ban");
                    }

                    // Create or update the ban
                    if (player.Ban is not null)
                    {
                        player.Ban.IsActive = true;
                        player.Ban.BanReason = request.BanReason;
                        player.Ban.BannedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var ban = new Ban
                        {
                            Player = player,
                            PlayerId = request.PlayerId,
                            BanReason = request.BanReason,
                            BannedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        _context.Bans.Add(ban);
                    }

                    // Add to the history
                    var banHistory = new BanHistoryEntity
                    {
                        Player = player,
                        PlayerId = request.PlayerId,
                        BanReason = request.BanReason,
                        BannedAt = DateTime.UtcNow
                    };

                    _context.BansHistory.Add(banHistory);

                    // Reject all pending withdrawals, except those whose payout may already exist: the payout notification settles them
                    var transactions = await _context.MoneyTransactions
                        .Where(mt => mt.PlayerId == request.PlayerId &&
                                    mt.TransactionTypeId == TransactionTypes.Withdrawal &&
                                    mt.TransactionStatusId == TransactionStatuses.Pending &&
                                    mt.ProcessingError != LiveTransactionProcessor.PayoutOutcomeUnknown)
                        .ToListAsync();

                    foreach (var t in transactions)
                    {
                        t.TransactionStatusId = TransactionStatuses.Rejected;
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("{Prefix} Player {PlayerId} banned", Prefix, request.PlayerId);
                    return Result.Success();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    attempt++;
                    _context.ChangeTracker.Clear();

                    if (attempt == MaxRetries)
                    {
                        _logger.LogError(ex, "{Prefix} Concurrency error while banning player {PlayerId} after {Attempt} attempts",
                            Prefix, request.PlayerId, attempt);

                        return Result.InternalError("Failed to complete the operation because of a data conflict");
                    }

                    await Task.Delay(100 * attempt);
                }
            }

            return Result.InternalError();
        }

        /// <inheritdoc/>
        public async Task<Result> UnbanPlayer(int playerId)
        {
            int attempt = 0;

            while (attempt < MaxRetries)
            {
                try
                {
                    var player = await _context.Players
                        .Include(p => p.Ban)
                        .AsTracking()
                        .FirstOrDefaultAsync(p => p.Id == playerId);

                    if (player is null)
                    {
                        _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                        return Result.NotFound($"Player with id {playerId} not found");
                    }

                    if (player.Ban is null || !player.Ban.IsActive)
                    {
                        _logger.LogWarning("{Prefix} Player {PlayerId} has no active ban", Prefix, playerId);
                        return Result.NotFound("User has no active ban");
                    }

                    // Update the ban
                    player.Ban.IsActive = false;

                    // Update the history
                    var lastBan = await _context.BansHistory
                        .Where(b => b.PlayerId == playerId && b.UnbannedAt == null)
                        .OrderByDescending(b => b.BannedAt)
                        .FirstOrDefaultAsync();

                    if (lastBan is not null)
                    {
                        lastBan.UnbannedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("{Prefix} Player {PlayerId} unbanned", Prefix, playerId);
                    return Result.Success();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    attempt++;
                    _context.ChangeTracker.Clear();

                    if (attempt == MaxRetries)
                    {
                        _logger.LogError(ex, "{Prefix} Concurrency error while unbanning player {PlayerId} after {Attempt} attempts",
                            Prefix, playerId, attempt);

                        return Result.InternalError("Failed to complete the operation because of a data conflict");
                    }

                    await Task.Delay(100 * attempt);
                }
            }

            return Result.InternalError();
        }

        #endregion Public Methods
    }
}
