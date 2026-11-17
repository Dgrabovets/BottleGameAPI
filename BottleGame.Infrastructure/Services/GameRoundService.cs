using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Service for working with game rounds
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="GameRoundService"/>
    /// </remarks>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger</param>
    /// <param name="cacheService">Cache service</param>
    public class GameRoundService(ApplicationDbContext context, ILogger<GameRoundService> logger, ICacheService cacheService) : IGameRoundService
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<GameRoundService> _logger = logger;

        /// <summary>
        /// Cache service
        /// </summary>
        private readonly ICacheService _cacheService = cacheService;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[GameRoundService]";

        #endregion Fields

        #region Public Methods

        /// <inheritdoc/>
        public async Task<Result> FinishGameRound(int playerId, FinishRoundRequest request, int? requestingPlayerId, bool isAdmin)
        {
            // Access check
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.GameRounds)
                .Include(p => p.Balance)
                .Include(p => p.Ban)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result.NotFound($"Player with id {playerId} not found");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} User is banned", Prefix);
                return Result.Forbidden("User is banned");
            }

            if (player.Balance.Amount < request.Bet && !request.IsWin)
            {
                _logger.LogWarning("{Prefix} Insufficient balance for withdrawal for player {PlayerId}", Prefix, playerId);
                return Result.Failure("Insufficient funds!");
            }

            var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .FirstOrDefaultAsync(s => s.PlayerId == playerId);

            if (roundsPlayedStat is null)
            {
                _logger.LogWarning("{Prefix} Statistics of the played rounds not found for player {PlayerId}", Prefix, playerId);
                return Result.Failure("Statistics of the played rounds of the player not found");
            }

            roundsPlayedStat.PlayedRoundsCount++;

            // Create the round record
            var round = new GameRound
            {
                Player = player,
                PlayerId = playerId,
                Bet = request.Bet,
                IsWin = request.IsWin,
                FinishedAt = DateTime.UtcNow
            };

            _context.GameRounds.Add(round);

            // Update the player balance
            player.Balance.Amount += request.IsWin ? request.Bet : -request.Bet;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // The balance was changed by another request after it was read, so the check above may be stale.
                _logger.LogWarning("{Prefix} Concurrent balance update for player {PlayerId}", Prefix, playerId);
                return Result.Conflict("The balance was changed by another request, try again");
            }

            _logger.LogInformation(
                "{Prefix} Round finished for player {PlayerId}. Status: {Status}, Amount: {Bet}",
                Prefix,
                playerId,
                request.IsWin ? "Win" : "Loss",
                request.Bet);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<GetPlayerPlayedRoundsResponse>> GetPlayerPlayedRounds(int playerId)
        {
            var player = await _context.Players
                .Include(p => p.GameRounds)
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result<GetPlayerPlayedRoundsResponse>.NotFound($"Player with id {playerId} not found");
            }

            var response = new GetPlayerPlayedRoundsResponse
            {
                Player = new PlayerDTO
                {
                    Id = playerId,
                    TelegramId = player.TelegramId,
                    Name = player.Name,
                    AvatarUrl = player.AvatarUrl,
                    RegisteredInAppAt = player.RegisteredInAppAt,
                    WinRate = player.WinRate,
                },
                Rounds = [.. player.GameRounds.Select(t => new RoundDTO
                {
                    Id = t.Id,
                    Bet = t.Bet,
                    IsWin = t.IsWin,
                    FinishedAt = t.FinishedAt
                })]
            };

            _logger.LogInformation("{Prefix} Got the list of rounds played by player {PlayerId}", Prefix, playerId);

            return Result<GetPlayerPlayedRoundsResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<GetProfitResponse>> GetProfit(GetProfitRequest request)
        {
            var query = _context.GameRounds.AsNoTracking().AsQueryable();

            if (request.DateFrom.HasValue)
            {
                query = query.Where(r => r.FinishedAt > request.DateFrom.Value);
            }

            if (request.DateTill.HasValue)
            {
                query = query.Where(r => r.FinishedAt < request.DateTill.Value);
            }

            // Check that data exists without loading all the records
            var hasRounds = await query.AnyAsync();

            if (!hasRounds)
            {
                _logger.LogWarning("{Prefix} Rounds of the players not found", Prefix);
                return Result<GetProfitResponse>.NotFound("Rounds of the players not found, the profit cannot be calculated");
            }

            // Use SQL aggregate functions instead of loading all the records into memory
            var profit = await query
                .Where(r => !r.IsWin)
                .SumAsync(r => r.Bet);

            var dateFrom = await query.MinAsync(r => r.FinishedAt);
            var dateTill = await query.MaxAsync(r => r.FinishedAt);

            _logger.LogInformation("{Prefix} Profit calculated for the period {DateFrom} - {DateTill}",
                Prefix, dateFrom, dateTill);

            var response = new GetProfitResponse
            {
                Profit = profit,
                DateFrom = dateFrom,
                DateTill = dateTill
            };

            return Result<GetProfitResponse>.Success(response);
        }

        #endregion Public Methods
    }
}
