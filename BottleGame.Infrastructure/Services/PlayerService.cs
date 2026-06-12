using Microsoft.Extensions.Logging;
using BottleGame.Application.Common;
using BottleGame.Infrastructure.Data;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using BottleGame.Application.Interfaces;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис для работы с игроками
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="PlayerService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    /// <param name="cacheService">Сервис кэширования</param>
    /// <param name="currencyService">Сервис валют и платёжных шлюзов</param>
    public class PlayerService(
        ApplicationDbContext context,
        ILogger<PlayerService> logger,
        ICacheService cacheService,
        ICurrencyService currencyService) : IPlayerService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<PlayerService> _logger = logger;

        /// <summary>
        /// Сервис кэширования
        /// </summary>
        private readonly ICacheService _cacheService = cacheService;

        /// <summary>
        /// Сервис валют и платёжных шлюзов
        /// </summary>
        private readonly ICurrencyService _currencyService = currencyService;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[PlayerService]";

        #endregion Fields

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
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<IsBannedResponse>.NotFound($"Игрок с ID {playerId} не найден");
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

            _logger.LogInformation("{Prefix} Проверка бана для игрока {PlayerId}. Результат: {IsBanned}",
                Prefix, playerId, response.IsBanned);

            return Result<IsBannedResponse>.Success(response);
        }

        #endregion Ban Methods

        #region Get Methods

        /// <inheritdoc/>
        public async Task<Result<PlayerInfoByIdResponse>> GetPlayerById(int playerId, int? requestingPlayerId, bool isAdmin)
        {
            // Проверка прав доступа
            if (!isAdmin && requestingPlayerId != playerId)
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
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<PlayerInfoByIdResponse>.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result<PlayerInfoByIdResponse>.Forbidden("Пользователь в бане");
            }

            var playersIdsInvited = player.InvitedByMe.Select(i => i.PlayerIdInvited).ToList();

            var invitedPlayers = await _context.Players
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

            _logger.LogInformation("{Prefix} Получены данные об игроке с PlayerId - {PlayerId}", Prefix, playerId);

            return Result<PlayerInfoByIdResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<PlayerInfoResponse>>> GetAllPlayers(long? telegramId = null,
                                                                                 string? name = null)
        {
            var isSearch = telegramId.HasValue || !string.IsNullOrWhiteSpace(name);

            var query = _context.Players
                .Include(p => p.GameRounds)
                .Include(p => p.Balance)
                .Include(p => p.InvitedByMe)
                .Include(p => p.Ban)
                .AsNoTracking()
                .AsQueryable();

            if (!isSearch)
            {
                query = query.WhereNotActivelyBanned();
            }
            else
            {
                if (telegramId.HasValue)
                {
                    query = query.Where(p => p.TelegramId == telegramId.Value);
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    var normalizedName = name.Trim().ToLowerInvariant();
                    query = query.Where(p => p.Name.ToLower().Contains(normalizedName));
                }
            }

            var players = await query.ToListAsync();

            if (players.Count == 0)
            {
                _logger.LogWarning("{Prefix} Игроки не найдены", Prefix);
                return Result<IEnumerable<PlayerInfoResponse>>.NotFound("Игроки не найдены");
            }

            var response = players.Select(MapToPlayerInfoResponse);

            _logger.LogInformation("{Prefix} Получены данные об игроках", Prefix);

            return Result<IEnumerable<PlayerInfoResponse>>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<PlayerInfoResponse>>> GetTop100Players()
        {
            var players = await _context.Players
                .Include(p => p.GameRounds)
                .Include(p => p.Balance)
                .Include(p => p.InvitedByMe)
                .Include(p => p.Ban)
                .WhereNotActivelyBanned()
                .OrderByDescending(p => p.Balance.Amount)
                .Take(100)
                .AsNoTracking()
                .ToListAsync();

            if (players.Count == 0)
            {
                _logger.LogWarning("{Prefix} Игроки не найдены", Prefix);
                return Result<IEnumerable<PlayerInfoResponse>>.NotFound("Игроки не найдены");
            }

            var response = players.Select(MapToPlayerInfoResponse);

            _logger.LogInformation("{Prefix} Получены данные о топ-100 игроках", Prefix);

            return Result<IEnumerable<PlayerInfoResponse>>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<PlayerStatisticsResponse>>> GetAllPlayersStatistics()
        {
            var players = await _context.Players
                .Include(p => p.Balance)
                .Include(p => p.GameRounds)
                .Include(p => p.Ban)
                .AsNoTracking()
                .ToListAsync();

            var response = players.Select(p => new PlayerStatisticsResponse
            {
                Player = MapToPlayerDTO(p),
                BannedAt = p.Ban is not null && p.Ban.IsActive ? p.Ban.BannedAt : null,
                Balance = p.Balance.Amount,
                RoundsQty = p.GameRounds.Count,
                WinsQty = p.GameRounds.Count(r => r.IsWin),
                LossQty = p.GameRounds.Count(r => !r.IsWin),
                WinAmount = p.GameRounds.Where(r => r.IsWin).Sum(r => r.Bet),
                LossAmount = p.GameRounds.Where(r => !r.IsWin).Sum(r => r.Bet)
            }).ToList();

            _logger.LogInformation("{Prefix} Получена статистика по {Count} игрокам", Prefix, response.Count);

            return Result<IEnumerable<PlayerStatisticsResponse>>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<PlayerStatisticsResponse>> GetPlayerStatistics(int playerId, int? requestingPlayerId, bool isAdmin)
        {
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<PlayerStatisticsResponse>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.Balance)
                .Include(p => p.GameRounds)
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<PlayerStatisticsResponse>.NotFound($"Игрок с ID {playerId} не найден");
            }

            var response = new PlayerStatisticsResponse
            {
                Player = MapToPlayerDTO(player),
                BannedAt = player.Ban is not null && player.Ban.IsActive ? player.Ban.BannedAt : null,
                Balance = player.Balance.Amount,
                RoundsQty = player.GameRounds.Count,
                WinsQty = player.GameRounds.Count(r => r.IsWin),
                LossQty = player.GameRounds.Count(r => !r.IsWin),
                WinAmount = player.GameRounds.Where(r => r.IsWin).Sum(r => r.Bet),
                LossAmount = player.GameRounds.Where(r => !r.IsWin).Sum(r => r.Bet)
            };

            _logger.LogInformation("{Prefix} Получена статистика по игроку - {PlayerId}", Prefix, playerId);

            return Result<PlayerStatisticsResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<PlayerRoundsStatisticsForWithdrawalDTO>> GetPlayerRoundsStatisticsForWithdrawal(
            int playerId, int? requestingPlayerId, bool isAdmin)
        {
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.Forbidden("Пользователь в бане");
            }

            var stat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .Include(s => s.MoneyTransaction)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.PlayerId == playerId);

            if (stat is null)
            {
                _logger.LogWarning("{Prefix} Статистика сыгранных раундов для игрока {PlayerId} не найдена", Prefix, playerId);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.NotFound($"Статистика сыгранных раундов для игрока {playerId} не найдена");
            }

            var transactionTypes = await _cacheService.GetTransactionTypesAsync();
            var transactionStatuses = await _cacheService.GetTransactionStatusesAsync();

            if (transactionStatuses.Count == 0 || transactionTypes.Count == 0)
            {
                _logger.LogWarning("{Prefix} Типы или статусы транзакций не найдены", Prefix);
                return Result<PlayerRoundsStatisticsForWithdrawalDTO>.InternalError("Типы или статусы транзакций не найдены");
            }

            TransactionDTO? lastDeposit = null;
            if (stat.MoneyTransaction is not null)
            {
                lastDeposit = new TransactionDTO
                {
                    Id = stat.MoneyTransaction.Id,
                    Amount = stat.MoneyTransaction.Amount,
                    StatusId = stat.MoneyTransaction.TransactionStatusId,
                    StatusName = transactionStatuses.GetValueOrDefault(stat.MoneyTransaction.TransactionStatusId),
                    TypeId = stat.MoneyTransaction.TransactionTypeId,
                    TypeName = transactionTypes.GetValueOrDefault(stat.MoneyTransaction.TransactionTypeId),
                    CreatedAt = stat.MoneyTransaction.CreatedAt,
                    ProcessedAt = stat.MoneyTransaction.ProcessedAt
                };
            }

            var dto = new PlayerRoundsStatisticsForWithdrawalDTO
            {
                Player = MapToPlayerDTO(player),
                LastDeposit = lastDeposit,
                TotalRoundsPlayed = stat.PlayedRoundsCount
            };

            return Result<PlayerRoundsStatisticsForWithdrawalDTO>.Success(dto);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>> GetAllPlayersRoundsStatisticsForWithdrawal()
        {
            var stats = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                .Include(s => s.MoneyTransaction)
                .Include(s => s.Player)
                .AsNoTracking()
                .ToListAsync();

            if (stats.Count == 0)
            {
                _logger.LogWarning("{Prefix} Статистика сыгранных раундов не найдена", Prefix);
                return Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>.NotFound("Статистика сыгранных раундов не найдена");
            }

            var transactionTypes = await _cacheService.GetTransactionTypesAsync();
            var transactionStatuses = await _cacheService.GetTransactionStatusesAsync();

            if (transactionStatuses.Count == 0 || transactionTypes.Count == 0)
            {
                _logger.LogWarning("{Prefix} Типы или статусы транзакций не найдены", Prefix);
                return Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>.InternalError("Типы или статусы транзакций не найдены");
            }

            var dtos = stats.Select(stat =>
            {
                var player = stat.Player;

                TransactionDTO? lastDeposit = null;
                if (stat.MoneyTransaction is not null)
                {
                    lastDeposit = new TransactionDTO
                    {
                        Id = stat.MoneyTransaction.Id,
                        Amount = stat.MoneyTransaction.Amount,
                        StatusId = stat.MoneyTransaction.TransactionStatusId,
                        StatusName = transactionStatuses.GetValueOrDefault(stat.MoneyTransaction.TransactionStatusId),
                        TypeId = stat.MoneyTransaction.TransactionTypeId,
                        TypeName = transactionTypes.GetValueOrDefault(stat.MoneyTransaction.TransactionTypeId),
                        CreatedAt = stat.MoneyTransaction.CreatedAt,
                        ProcessedAt = stat.MoneyTransaction.ProcessedAt
                    };
                }

                return new PlayerRoundsStatisticsForWithdrawalDTO
                {
                    Player = MapToPlayerDTO(player),
                    LastDeposit = lastDeposit,
                    TotalRoundsPlayed = stat.PlayedRoundsCount
                };
            });

            return Result<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>.Success(dtos);
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
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result.NotFound($"Игрок с ID {playerId} не найден");
            }

            // Изменение базовых данных
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                player.AvatarUrl = request.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(request.Name))
                player.Name = request.Name;

            if (request.WinChance != default)
                player.PlayerOptions.WinChance = request.WinChance!.Value;

            // Валидация и изменение валюты, метода оплаты и типа реквизитов (через API процессинга)
            if (!string.IsNullOrWhiteSpace(request.Currency) && !string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                // Проверяем валюту через API процессинга
                var isValidCurrency = await _currencyService.IsCurrencyActiveAsync(request.Currency);
                if (!isValidCurrency)
                {
                    var availableCurrencies = await _currencyService.GetAvailableCurrenciesAsync();
                    var currencyCodes = string.Join(", ", availableCurrencies.Select(c => c.Code));
                    _logger.LogWarning("{Prefix} Попытка установить недопустимую валюту: {Currency}", Prefix, request.Currency);
                    return Result.Failure($"Валюта '{request.Currency}' не поддерживается. Доступные: {currencyCodes}");
                }

                // Проверяем платёжный шлюз для указанной валюты
                var paymentGateways = await _currencyService.GetPaymentGatewaysForCurrencyAsync(request.Currency);
                var selectedGateway = paymentGateways.FirstOrDefault(g => g.Code.Equals(request.PaymentMethod, StringComparison.OrdinalIgnoreCase));
                if (selectedGateway is null)
                {
                    var gatewayCodes = string.Join(", ", paymentGateways.Select(g => g.Code));
                    _logger.LogWarning("{Prefix} Попытка установить недопустимый метод оплаты: {PaymentMethod}", Prefix, request.PaymentMethod);
                    return Result.Failure($"Метод оплаты '{request.PaymentMethod}' не поддерживается для валюты '{request.Currency}'. Доступные: {gatewayCodes}");
                }

                // Определяем тип реквизитов (из запроса или по умолчанию)
                var detailType = !string.IsNullOrWhiteSpace(request.PaymentDetailType)
                    ? request.PaymentDetailType.ToLowerInvariant()
                    : (selectedGateway.DetailTypes.Count > 0 ? selectedGateway.DetailTypes[0] : "card");

                // Проверяем, что тип реквизитов поддерживается выбранным шлюзом
                if (selectedGateway.DetailTypes.Count > 0 &&
                    !selectedGateway.DetailTypes.Any(dt => dt.Equals(detailType, StringComparison.OrdinalIgnoreCase)))
                {
                    var availableTypes = string.Join(", ", selectedGateway.DetailTypes);
                    _logger.LogWarning("{Prefix} Попытка установить недопустимый тип реквизитов: {DetailType}", Prefix, detailType);
                    return Result.Failure($"Тип реквизитов '{detailType}' не поддерживается методом оплаты '{request.PaymentMethod}'. Доступные: {availableTypes}");
                }

                player.PlayerOptions.Currency = request.Currency.ToLowerInvariant();
                player.PlayerOptions.PaymentMethod = request.PaymentMethod.ToLowerInvariant();
                player.PlayerOptions.PaymentDetailType = detailType;
            }
            else if ((!string.IsNullOrWhiteSpace(request.Currency) && string.IsNullOrWhiteSpace(request.PaymentMethod)) ||
                     (string.IsNullOrWhiteSpace(request.Currency) && !string.IsNullOrWhiteSpace(request.PaymentMethod)))
            {
                return Result.Failure("Поля Currency и PaymentMethod не могут изменяться отдельно друг от друга");
            }
            else if (!string.IsNullOrWhiteSpace(request.PaymentDetailType))
            {
                // Обновление только PaymentDetailType (без изменения метода оплаты)
                var currentPaymentMethod = player.PlayerOptions.PaymentMethod;
                var isValidDetailType = await _currencyService.IsDetailTypeValidForGatewayAsync(currentPaymentMethod, request.PaymentDetailType);
                if (!isValidDetailType)
                {
                    var availableTypes = await _currencyService.GetDetailTypesForGatewayAsync(currentPaymentMethod);
                    var typesStr = availableTypes.Any() ? string.Join(", ", availableTypes) : "любой";
                    _logger.LogWarning("{Prefix} Попытка установить недопустимый тип реквизитов: {DetailType}", Prefix, request.PaymentDetailType);
                    return Result.Failure($"Тип реквизитов '{request.PaymentDetailType}' не поддерживается методом оплаты '{currentPaymentMethod}'. Доступные: {typesStr}");
                }

                player.PlayerOptions.PaymentDetailType = request.PaymentDetailType.ToLowerInvariant();
            }

            // Валидация и изменение кошелька и метода выплаты
            if (!string.IsNullOrWhiteSpace(request.Wallet) && !string.IsNullOrWhiteSpace(request.PayoutMethod))
            {
                // Получаем текущую валюту игрока для проверки шлюза выплат
                var currency = !string.IsNullOrWhiteSpace(request.Currency)
                    ? request.Currency
                    : player.PlayerOptions.Currency ?? "rub";

                var payoutGateways = await _currencyService.GetPayoutGatewaysForCurrencyAsync(currency);
                var isValidPayoutGateway = payoutGateways.Any(g => g.Code.Equals(request.PayoutMethod, StringComparison.OrdinalIgnoreCase));
                if (!isValidPayoutGateway)
                {
                    var gatewayCodes = string.Join(", ", payoutGateways.Select(g => g.Code));
                    _logger.LogWarning("{Prefix} Попытка установить недопустимый метод выплаты: {PayoutMethod}", Prefix, request.PayoutMethod);
                    return Result.Failure($"Метод выплаты '{request.PayoutMethod}' не поддерживается для валюты '{currency}'. Доступные: {gatewayCodes}");
                }

                player.PlayerOptions.Wallet = request.Wallet;
                player.PlayerOptions.PayoutMethod = request.PayoutMethod.ToLowerInvariant();
            }
            else if ((!string.IsNullOrWhiteSpace(request.Wallet) && string.IsNullOrWhiteSpace(request.PayoutMethod)) ||
                     (string.IsNullOrWhiteSpace(request.Wallet) && !string.IsNullOrWhiteSpace(request.PayoutMethod)))
            {
                return Result.Failure("Поля Wallet и PayoutMethod не могут изменяться отдельно друг от друга");
            }

            // Изменение баланса (только для админа)
            if (isAdmin && request.Balance is not null)
            {
                var amountToUpdateBalance = player.Balance.Amount - request.Balance.Value;

                player.Balance.Amount = request.Balance.Value;

                var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
                if (appBalance is null)
                {
                    _logger.LogWarning("{Prefix} Не найдены данные баланса приложения", Prefix);
                    return Result.Failure("Не найдены данные баланса приложения");
                }

                appBalance.Amount -= amountToUpdateBalance;

                appBalance.BalanceAmountUpdatedAt = DateTime.UtcNow;
                appBalance.BalanceAmountAdminIdUpdatedBy = 1;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Игрок {PlayerId} изменен", Prefix, playerId);

            return Result.Success();
        }

        #endregion Update Methods

        #region Private Methods

        /// <summary>
        /// Маппинг Player в PlayerInfoResponse
        /// </summary>
        private static PlayerInfoResponse MapToPlayerInfoResponse(Player player) =>
            new()
            {
                Player = MapToPlayerDTO(player),
                Balance = player.Balance.Amount,
                Rounds = [.. player.GameRounds.Select(MapToRoundDTO)],
                BannedAt = player.Ban is not null && player.Ban.IsActive ? player.Ban.BannedAt : null,
                PlayersIdsInvited = [.. player.InvitedByMe.Select(pi => pi.PlayerIdInvited)],
            };

        /// <summary>
        /// Маппинг Player в PlayerDTO
        /// </summary>
        private static PlayerDTO MapToPlayerDTO(Player player)
        {
            return new PlayerDTO
            {
                Id = player.Id,
                TelegramId = player.TelegramId,
                Name = player.Name,
                AvatarUrl = player.AvatarUrl,
                RegisteredInAppAt = player.RegisteredInAppAt,
                WinRate = player.WinRate
            };
        }

        /// <summary>
        /// Маппинг GameRound в RoundDTO
        /// </summary>
        private static RoundDTO MapToRoundDTO(GameRound round)
        {
            return new RoundDTO
            {
                Id = round.Id,
                Bet = round.Bet,
                IsWin = round.IsWin,
                FinishedAt = round.FinishedAt
            };
        }

        #endregion Private Methods
    }
}




