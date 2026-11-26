using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static BottleGame.Infrastructure.Services.PlayerMapper;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Service for working with players
    /// </summary>
    public class PlayerService : IPlayerService
    {
        #region Fields

        /// <summary>
        /// Database context
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger<PlayerService> _logger;

        /// <summary>
        /// Service of currencies and payment gateways
        /// </summary>
        private readonly ICurrencyService _currencyService;

        /// <summary>
        /// Log prefix
        /// </summary>
        private const string Prefix = "[PlayerService]";

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="PlayerService"/>
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="logger">Logger</param>
        /// <param name="currencyService">Service of currencies and payment gateways</param>
        public PlayerService(ApplicationDbContext context, ILogger<PlayerService> logger, ICurrencyService currencyService)
        {
            _context = context;
            _logger = logger;
            _currencyService = currencyService;
        }

        #region Ban Methods

        /// <inheritdoc/>
        public async Task<Result<IsBannedResponse>> IsBanned(int playerId)
        {
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

        #endregion Ban Methods

        #region Get Methods

        /// <inheritdoc/>
        public async Task<Result<PlayerInfoByIdResponse>> GetPlayerById(int playerId, int? requestingPlayerId, bool isStaff)
        {
            // Access check
            if (!isStaff && requestingPlayerId != playerId)
            {
                return Result<PlayerInfoByIdResponse>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.GameRounds)
                .Include(p => p.Balance)
                .Include(p => p.InvitedByMe)
                .Include(p => p.Ban)
                .Include(p => p.PlayerOptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result<PlayerInfoByIdResponse>.NotFound($"Player with id {playerId} not found");
            }

            if (!isStaff && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} User is banned", Prefix);
                return Result<PlayerInfoByIdResponse>.Forbidden("User is banned");
            }

            var playersIdsInvited = player.InvitedByMe.Select(i => i.PlayerIdInvited).ToList();

            // The rounds are needed for the win rate of each invited player
            var invitedPlayers = await _context.Players
                .Include(p => p.GameRounds)
                .Where(p => playersIdsInvited.Contains(p.Id))
                .AsNoTracking()
                .ToListAsync();

            var response = new PlayerInfoByIdResponse
            {
                Player = MapToPlayerDTO(player),
                Balance = player.Balance.Amount,
                Rounds = [.. player.GameRounds.Select(MapToRoundDTO)],
                BannedAt = player.Ban is not null && player.Ban.IsActive ? player.Ban.BannedAt : null,
                Options = new PlayerOptionsDTO
                {
                    WinChance = player.PlayerOptions.WinChance,
                    Currency = player.PlayerOptions.Currency ?? string.Empty,
                    Wallet = player.PlayerOptions.Wallet,
                    PayoutMethod = player.PlayerOptions.PayoutMethod,
                    PaymentMethod = player.PlayerOptions.PaymentMethod,
                    PaymentDetailType = player.PlayerOptions.PaymentDetailType
                },
                PlayersInvited = invitedPlayers.Select(MapToPlayerDTO)
            };

            _logger.LogInformation("{Prefix} Got the data of the player with PlayerId - {PlayerId}", Prefix, playerId);

            return Result<PlayerInfoByIdResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<PlayerInfoResponse>>> GetAllPlayers(long? telegramId = null,
                                                                                 string? name = null,
                                                                                 PageRequest? page = null)
        {
            var isSearch = telegramId.HasValue || !string.IsNullOrWhiteSpace(name);

            var query = (await FilterPlayersAsync(telegramId, name))
                .Include(p => p.GameRounds)
                .Include(p => p.Balance)
                .Include(p => p.InvitedByMe)
                .Include(p => p.Ban)
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .AsQueryable();

            if (page is not null)
            {
                query = query.Skip(page.Skip).Take(page.PageSize);
            }

            var players = await query.ToListAsync();

            if (players.Count == 0)
            {
                // An empty page is not an error, only an empty list without paging is
                if (isSearch || page is not null)
                {
                    return Result<IEnumerable<PlayerInfoResponse>>.Success([]);
                }

                _logger.LogWarning("{Prefix} Players not found", Prefix);
                return Result<IEnumerable<PlayerInfoResponse>>.NotFound("Players not found");
            }

            var response = players.Select(MapToPlayerInfoResponse);

            _logger.LogInformation("{Prefix} Got the data of the players", Prefix);

            return Result<IEnumerable<PlayerInfoResponse>>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<int> CountPlayers(long? telegramId = null, string? name = null) =>
            await (await FilterPlayersAsync(telegramId, name)).CountAsync();

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<PlayerInfoResponse>>> GetTop100Players()
        {
            var bannedPlayerIds = await _context.GetActivelyBannedPlayerIdsAsync();

            var players = await _context.Players
                .Include(p => p.GameRounds)
                .Include(p => p.Balance)
                .Include(p => p.InvitedByMe)
                .Include(p => p.Ban)
                .WhereNotActivelyBanned(bannedPlayerIds)
                .OrderByDescending(p => p.Balance.Amount)
                .Take(100)
                .AsNoTracking()
                .ToListAsync();

            if (players.Count == 0)
            {
                _logger.LogWarning("{Prefix} Players not found", Prefix);
                return Result<IEnumerable<PlayerInfoResponse>>.NotFound("Players not found");
            }

            var response = players.Select(MapToPlayerInfoResponse);

            _logger.LogInformation("{Prefix} Got the data of the top 100 players", Prefix);

            return Result<IEnumerable<PlayerInfoResponse>>.Success(response);
        }

        /// <summary>
        /// Builds the player query of the list: without a search actively banned players are left out,
        /// with a search the telegram id and the name decide and banned players are included
        /// </summary>
        /// <param name="telegramId">Exact Telegram id</param>
        /// <param name="name">Partial match on the nickname</param>
        /// <returns>Filtered query without includes and without ordering</returns>
        private async Task<IQueryable<Player>> FilterPlayersAsync(long? telegramId, string? name)
        {
            var query = _context.Players.AsQueryable();

            if (!telegramId.HasValue && string.IsNullOrWhiteSpace(name))
            {
                var bannedPlayerIds = await _context.GetActivelyBannedPlayerIdsAsync();
                return query.WhereNotActivelyBanned(bannedPlayerIds);
            }

            if (telegramId.HasValue)
            {
                query = query.Where(p => p.TelegramId == telegramId.Value);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var normalizedName = name.Trim().ToLowerInvariant();
                query = query.Where(p => p.Name.ToLower().Contains(normalizedName));
            }

            return query;
        }

        #endregion Get Methods

        #region Update Methods

        /// <inheritdoc/>
        public async Task<Result> EditPlayer(int playerId, EditPlayerRequest request, bool isAdmin)
        {
            var player = await _context.Players
                .Include(p => p.PlayerOptions)
                .Include(p => p.Balance)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Player with id {PlayerId} not found", Prefix, playerId);
                return Result.NotFound($"Player with id {playerId} not found");
            }

            // Change of the basic data
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                player.AvatarUrl = request.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(request.Name))
                player.Name = request.Name;

            // Win chance (admin only), otherwise a player could raise their own chance
            if (isAdmin && request.WinChance is not null)
                player.PlayerOptions.WinChance = request.WinChance.Value;

            // Validation and change of the currency, payment method and detail type (through the processing API)
            if (!string.IsNullOrWhiteSpace(request.Currency) && !string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                // Check the currency through the processing API
                var isValidCurrency = await _currencyService.IsCurrencyActiveAsync(request.Currency);
                if (!isValidCurrency)
                {
                    var availableCurrencies = await _currencyService.GetAvailableCurrenciesAsync();
                    var currencyCodes = string.Join(", ", availableCurrencies.Select(c => c.Code));
                    _logger.LogWarning("{Prefix} Attempt to set an invalid currency: {Currency}", Prefix, LogSanitizer.Clean(request.Currency));
                    return Result.Failure($"Currency '{request.Currency}' is not supported. Available: {currencyCodes}");
                }

                // Check the payment gateway for the given currency
                var paymentGateways = await _currencyService.GetPaymentGatewaysForCurrencyAsync(request.Currency);
                var selectedGateway = paymentGateways.FirstOrDefault(g => g.Code.Equals(request.PaymentMethod, StringComparison.OrdinalIgnoreCase));
                if (selectedGateway is null)
                {
                    var gatewayCodes = string.Join(", ", paymentGateways.Select(g => g.Code));
                    _logger.LogWarning("{Prefix} Attempt to set an invalid payment method: {PaymentMethod}", Prefix, LogSanitizer.Clean(request.PaymentMethod));
                    return Result.Failure($"Payment method '{request.PaymentMethod}' is not supported for currency '{request.Currency}'. Available: {gatewayCodes}");
                }

                // Determine the detail type (from the request or the default)
                var detailType = !string.IsNullOrWhiteSpace(request.PaymentDetailType)
                                   ? request.PaymentDetailType.ToLowerInvariant()
                                   : (selectedGateway.DetailTypes.Count > 0 ? selectedGateway.DetailTypes[0] : "card");

                // Check that the detail type is supported by the selected gateway
                if (selectedGateway.DetailTypes.Count > 0 &&
                    !selectedGateway.DetailTypes.Any(dt => dt.Equals(detailType, StringComparison.OrdinalIgnoreCase)))
                {
                    var availableTypes = string.Join(", ", selectedGateway.DetailTypes);
                    _logger.LogWarning("{Prefix} Attempt to set an invalid detail type: {DetailType}", Prefix, LogSanitizer.Clean(detailType));
                    return Result.Failure($"Detail type '{detailType}' is not supported by payment method '{request.PaymentMethod}'. Available: {availableTypes}");
                }

                player.PlayerOptions.Currency = request.Currency.ToLowerInvariant();
                player.PlayerOptions.PaymentMethod = request.PaymentMethod.ToLowerInvariant();
                player.PlayerOptions.PaymentDetailType = detailType;
            }
            else if ((!string.IsNullOrWhiteSpace(request.Currency) && string.IsNullOrWhiteSpace(request.PaymentMethod)) ||
                     (string.IsNullOrWhiteSpace(request.Currency) && !string.IsNullOrWhiteSpace(request.PaymentMethod)))
            {
                return Result.Failure("The fields Currency and PaymentMethod cannot be changed separately from each other");
            }
            else if (!string.IsNullOrWhiteSpace(request.PaymentDetailType))
            {
                // Update only PaymentDetailType (without changing the payment method)
                var currentPaymentMethod = player.PlayerOptions.PaymentMethod;
                var isValidDetailType = await _currencyService.IsDetailTypeValidForGatewayAsync(currentPaymentMethod, request.PaymentDetailType);
                if (!isValidDetailType)
                {
                    var availableTypes = await _currencyService.GetDetailTypesForGatewayAsync(currentPaymentMethod);
                    var typesStr = availableTypes.Any() ? string.Join(", ", availableTypes) : "any";
                    _logger.LogWarning("{Prefix} Attempt to set an invalid detail type: {DetailType}", Prefix, LogSanitizer.Clean(request.PaymentDetailType));
                    return Result.Failure($"Detail type '{request.PaymentDetailType}' is not supported by payment method '{currentPaymentMethod}'. Available: {typesStr}");
                }

                player.PlayerOptions.PaymentDetailType = request.PaymentDetailType.ToLowerInvariant();
            }

            // Validation and change of the wallet and the payout method
            if (!string.IsNullOrWhiteSpace(request.Wallet) && !string.IsNullOrWhiteSpace(request.PayoutMethod))
            {
                // Get the current currency of the player to check the payout gateway
                var currency = !string.IsNullOrWhiteSpace(request.Currency)
                                 ? request.Currency
                                 : player.PlayerOptions.Currency ?? "rub";

                var payoutGateways = await _currencyService.GetPayoutGatewaysForCurrencyAsync(currency);
                var isValidPayoutGateway = payoutGateways.Any(g => g.Code.Equals(request.PayoutMethod, StringComparison.OrdinalIgnoreCase));
                if (!isValidPayoutGateway)
                {
                    var gatewayCodes = string.Join(", ", payoutGateways.Select(g => g.Code));
                    _logger.LogWarning("{Prefix} Attempt to set an invalid payout method: {PayoutMethod}", Prefix, LogSanitizer.Clean(request.PayoutMethod));
                    return Result.Failure($"Payout method '{request.PayoutMethod}' is not supported for currency '{currency}'. Available: {gatewayCodes}");
                }

                player.PlayerOptions.Wallet = request.Wallet;
                player.PlayerOptions.PayoutMethod = request.PayoutMethod.ToLowerInvariant();
            }
            else if ((!string.IsNullOrWhiteSpace(request.Wallet) && string.IsNullOrWhiteSpace(request.PayoutMethod)) ||
                     (string.IsNullOrWhiteSpace(request.Wallet) && !string.IsNullOrWhiteSpace(request.PayoutMethod)))
            {
                return Result.Failure("The fields Wallet and PayoutMethod cannot be changed separately from each other");
            }

            // Change of the balance (admin only)
            if (isAdmin && request.Balance is not null)
            {
                var amountToUpdateBalance = player.Balance.Amount - request.Balance.Value;

                player.Balance.Amount = request.Balance.Value;

                var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
                if (appBalance is null)
                {
                    _logger.LogWarning("{Prefix} Application balance data not found", Prefix);
                    return Result.Failure("Application balance data not found");
                }

                appBalance.Amount -= amountToUpdateBalance;

                appBalance.BalanceAmountUpdatedAt = DateTime.UtcNow;
                appBalance.BalanceAmountAdminIdUpdatedBy = 1;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Player {PlayerId} changed", Prefix, playerId);

            return Result.Success();
        }

        #endregion Update Methods
    }
}
