using Microsoft.Extensions.Logging;
using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Processing;
using BottleGame.Domain.Constants;
using BottleGame.Infrastructure.Data;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.External.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BottleGame.Application.Interfaces;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Сервис для работы с транзакциями
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="TransactionService"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    /// <param name="processingService">Сервис процессинга</param>
    /// <param name="cacheService">Сервис кэширования</param>
    /// <param name="options">Настройки процессинга</param>
    public class TransactionService(
        ApplicationDbContext context,
        ILogger<TransactionService> logger,
        IProcessingService processingService,
        ICacheService cacheService,
        IOptions<ProcessingOptions> options) : ITransactionService
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<TransactionService> _logger = logger;

        /// <summary>
        /// Сервис процессинга
        /// </summary>
        private readonly IProcessingService _processingService = processingService;

        /// <summary>
        /// Сервис кэширования
        /// </summary>
        private readonly ICacheService _cacheService = cacheService;

        /// <summary>
        /// Настройки процессинга
        /// </summary>
        private readonly ProcessingOptions _options = options.Value;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[TransactionService]";

        #endregion Fields

        #region Balance Methods

        /// <inheritdoc/>
        public async Task<Result<GetPlayerBalanceResponse>> GetPlayerBalance(int playerId, int? requestingPlayerId, bool isAdmin)
        {
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<GetPlayerBalanceResponse>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<GetPlayerBalanceResponse>.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result<GetPlayerBalanceResponse>.Forbidden("Пользователь в бане");
            }

            var balance = await _context.PlayersBalances
                .AsNoTracking()
                .Where(b => b.PlayerId == playerId)
                .Select(b => b.Amount)
                .FirstOrDefaultAsync();

            if (balance == default)
            {
                _logger.LogError("{Prefix} Баланс игрока не найден! Нарушена целостность данных", Prefix);
            }

            _logger.LogInformation("{Prefix} Успешно получен баланс для игрока {PlayerId}: {Amount}", Prefix, playerId, balance);

            return Result<GetPlayerBalanceResponse>.Success(new GetPlayerBalanceResponse { PlayerId = playerId, Balance = balance });
        }

        #endregion Balance Methods

        #region Transaction Methods

        /// <inheritdoc/>
        public async Task<Result<object?>> CreateTransaction(int playerId, MoneyTransactionRequest request, int? requestingPlayerId, bool isAdmin)
        {
            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<object?>.Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.PlayerOptions)
                .Include(p => p.Balance)
                .Include(p => p.Ban)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<object?>.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result<object?>.Forbidden("Пользователь в бане");
            }

            if (!request.IsDeposit && player.Balance.Amount < request.Amount)
            {
                _logger.LogWarning("{Prefix} Недостаточно средств на балансе для снятия у игрока {PlayerId}", Prefix, playerId);
                return Result<object?>.Failure("Недостаточно средств!");
            }

            var gameParams = await _cacheService.GetGameParamsAsync();
            if (gameParams is null)
            {
                _logger.LogWarning("{Prefix} Не найдены игровые параметры", Prefix);
                return Result<object?>.Failure("Не найдены игровые параметры");
            }

            // До подключение процессинговой площадки отрубаем
            //var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
            //    .FirstOrDefaultAsync(s => s.PlayerId == playerId);

            //if (roundsPlayedStat is null)
            //{
            //    _logger.LogWarning("{Prefix} Не найдены данные статистики сыгранных раундов для игрока {PlayerId}", Prefix, playerId);
            //    return Result<object?>.Failure("Не найдены данные статистики сыгранных раундов игрока");
            //}

            //if (!request.IsDeposit && roundsPlayedStat.PlayedRoundsCount < gameParams.MinRoundsCountForWithdrawal)
            //{
            //    _logger.LogWarning("{Prefix} Недостаточно сыгранных раундов для вывода у игрока {PlayerId}", Prefix, playerId);
            //    return Result<object?>.Failure($"Для вывода необходимо сыграть минимум {gameParams.MinRoundsCountForWithdrawal} раундов. Сейчас сыграно: {roundsPlayedStat.PlayedRoundsCount}");
            //}

            var transactionType = await _context.TransactionTypes.FirstOrDefaultAsync(t => t.Id == (request.IsDeposit ? TransactionTypes.Deposit : TransactionTypes.Withdrawal));
            var transactionStatus = await _context.TransactionStatuses.FirstOrDefaultAsync(s => s.Id == TransactionStatuses.Pending);

            var transaction = new MoneyTransaction
            {
                Player = player,
                PlayerId = playerId,
                Amount = request.Amount,
                TransactionTypeId = transactionType!.Id,
                TransactionType = transactionType,
                TransactionStatusId = transactionStatus!.Id,
                TransactionStatus = transactionStatus,
                CreatedAt = DateTime.UtcNow
            };

            _context.MoneyTransactions.Add(transaction);

            if (!request.IsDeposit)
            {
                player.Balance.Amount -= request.Amount;
            }

            await _context.SaveChangesAsync();

            object? responseData = null;

            if (request.IsDeposit)
            {
                // До подключения процессинговой площадки отрубаем — вернуть блок CreateOrderAsync перед продом
                //try
                //{
                //    _logger.LogInformation("{Prefix} Создание запроса для платежа игрока {PlayerId}, сумма {Amount}", Prefix, playerId, request.Amount);
                //
                //    var createOrderRequest = new CreateOrderRequest(
                //        _options.MerchantId,
                //        transaction.Id.ToString(),
                //        request.Amount,
                //        player.PlayerOptions.PaymentMethod.ToString(),
                //        "card",
                //        player.TelegramId.ToString());
                //
                //    var orderResponse = await _processingService.CreateOrderAsync(createOrderRequest);
                //
                //    if (!orderResponse.IsSuccess || string.IsNullOrEmpty(orderResponse.PaymentUrl))
                //    {
                //        transaction.TransactionStatusId = TransactionStatuses.Rejected;
                //        transaction.IsProcessingSuccess = false;
                //        transaction.ProcessingError = orderResponse.ErrorMessage;
                //        await _context.SaveChangesAsync();
                //
                //        _logger.LogError("{Prefix} Ошибка при создании платежа для игрока {PlayerId}: {ErrorMessage}", Prefix, playerId, orderResponse.ErrorMessage);
                //        return Result<object?>.InternalError("Ошибка при создании платежа: " + orderResponse.ErrorMessage);
                //    }
                //
                //    transaction.ProcessingOrderId = orderResponse.OrderId;
                //    transaction.PaymentUrl = orderResponse.PaymentUrl;
                //    transaction.PaymentGateway = player.PlayerOptions.PaymentMethod.ToString();
                //    transaction.Currency = player.PlayerOptions.Currency?.ToString() ?? "rub";
                //    await _context.SaveChangesAsync();
                //
                //    responseData = new
                //    {
                //        PaymentLink = orderResponse.PaymentUrl,
                //        orderResponse.OrderId
                //    };
                //}
                //catch (Exception ex)
                //{
                //    transaction.TransactionStatusId = TransactionStatuses.Rejected;
                //    transaction.IsProcessingSuccess = false;
                //    await _context.SaveChangesAsync();
                //
                //    _logger.LogError(ex, "{Prefix} Ошибка при создании платежа для игрока {PlayerId}", Prefix, playerId);
                //    return Result<object?>.InternalError("Ошибка при создании платежа");
                //}

                responseData = await CompleteDepositLocallyForTestsAsync(transaction, player, request.Amount);
            }

            _logger.LogInformation(
                "{Prefix} Транзакция создана для игрока {PlayerId}. Тип: {Status}, Сумма: {Bet}",
                Prefix,
                playerId,
                request.IsDeposit ? "Пополнение" : "Вывод",
                request.Amount);

            return Result<object?>.Success(responseData);
        }

        /// <inheritdoc/>
        public async Task<Result> UpdateTransactionStatus(int transactionId, ChangeTransactionStatusRequest request)
        {
            var inWaitStatusId = TransactionStatuses.Pending;
            var rejectedStatusId = TransactionStatuses.Rejected;
            var approvedStatusId = TransactionStatuses.Approved;
            var withdrawalTypeId = TransactionTypes.Withdrawal;

            if (request.TransactionStatusId == inWaitStatusId)
            {
                _logger.LogWarning("{Prefix} Некорректный статус транзакции: {StatusId}", Prefix, request.TransactionStatusId);
                return Result.Failure($"Допустимые статусы: {approvedStatusId}-Одобрено, {rejectedStatusId}-Отклонено");
            }

            var transaction = await _context.MoneyTransactions
                .Include(t => t.Player).ThenInclude(p => p.PlayerOptions)
                .Include(t => t.Player).ThenInclude(p => p.Balance)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction is null)
            {
                _logger.LogWarning("{Prefix} Транзакция с ID {TransactionId} не найдена", Prefix, transactionId);
                return Result.NotFound($"Транзакция с ID {transactionId} не найдена");
            }

            if (transaction.Player is null || transaction.Player.Balance is null || transaction.Player.PlayerOptions is null)
            {
                _logger.LogWarning("{Prefix} Данные транзакции {TransactionId} неполные", Prefix, transaction.Id);
                return Result.NotFound($"Данные транзакции {transaction.Id} неполные");
            }

            if (transaction.TransactionTypeId != withdrawalTypeId)
            {
                _logger.LogWarning("{Prefix} Тип транзакции {TransactionId} не поддерживается для ручного одобрения/отклонения", Prefix, transactionId);
                return Result.Failure("Возможно изменять статус только у транзакций с типом \"Вывод\"");
            }

            if (transaction.TransactionStatusId != inWaitStatusId)
            {
                _logger.LogWarning("{Prefix} Транзакция {TransactionId} уже обработана (статус {StatusId})", Prefix, transactionId, transaction.TransactionStatusId);
                return Result.Failure("Нельзя изменить статус уже обработанной транзакции");
            }

            if (transaction.TransactionStatusId == request.TransactionStatusId)
            {
                _logger.LogWarning("{Prefix} Транзакция {TransactionId} уже имеет статус {StatusId}", Prefix, transactionId, request.TransactionStatusId);
                return Result.Failure("Транзакция уже имеет указанный статус");
            }

            var gameParams = await _cacheService.GetGameParamsAsync();
            if (gameParams is null)
            {
                return Result.Failure("Не найдены игровые параметры");
            }

            // До подключения процессинговой площадки отрубаем
            //var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
            //    .FirstOrDefaultAsync(s => s.PlayerId == transaction.PlayerId);

            //if (roundsPlayedStat is null)
            //{
            //    return Result.Failure("Не найдены данные статистики сыгранных раундов игрока");
            //}

            var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
            if (appBalance is null)
            {
                return Result.Failure("Не найдены данные баланса приложения");
            }

            if (request.TransactionStatusId == rejectedStatusId)
            {
                transaction.Player.Balance.Amount += transaction.Amount;
                transaction.TransactionStatusId = request.TransactionStatusId;
            }
            else if (request.TransactionStatusId == approvedStatusId)
            {
                // До подключения процессинговой площадки отрубаем
                //if (roundsPlayedStat.PlayedRoundsCount < gameParams.MinRoundsCountForWithdrawal)
                //{
                //    _logger.LogWarning("{Prefix} Недостаточно сыгранных раундов для вывода у игрока {PlayerId}", Prefix, transaction.PlayerId);
                //    return Result.Failure($"Для вывода необходимо сыграть минимум {gameParams.MinRoundsCountForWithdrawal} раундов. Сейчас сыграно: {roundsPlayedStat.PlayedRoundsCount}");
                //}

                //if (transaction.Amount > appBalance.LowerThreshold * 0.1m)
                //{
                //    _logger.LogWarning("{Prefix} Одобрение транзакции невозможно - сумма превышает порог", Prefix);
                //    return Result.Failure("Одобрение транзакции невозможно - сумма к выводу превышает нижний порог баланса приложения!");
                //}

                // До подключения процессинговой площадки отрубаем — вернуть блок CreatePoolPayoutAsync перед продом
                //try
                //{
                //    var createPoolPayoutRequest = new CreatePoolPayoutRequest(
                //        _options.MerchantId,
                //        transaction.Id.ToString(),
                //        transaction.Amount,
                //        transaction.Player.PlayerOptions.PayoutMethod.ToString(),
                //        transaction.Player.PlayerOptions.Wallet!,
                //        transaction.Player.PlayerOptions.Currency?.ToString() ?? "rub",
                //        30);
                //
                //    var payoutResponse = await _processingService.CreatePoolPayoutAsync(createPoolPayoutRequest);
                //
                //    if (!payoutResponse.IsSuccess)
                //    {
                //        transaction.Player.Balance.Amount += transaction.Amount;
                //        transaction.TransactionStatusId = TransactionStatuses.Rejected;
                //        transaction.IsProcessingSuccess = false;
                //        transaction.ProcessingError = payoutResponse.ErrorMessage;
                //        transaction.ProcessedAt = DateTime.UtcNow;
                //        await _context.SaveChangesAsync();
                //
                //        _logger.LogError("{Prefix} Ошибка при создании выплаты для игрока {PlayerId}: {ErrorMessage}", Prefix, transaction.PlayerId, payoutResponse.ErrorMessage);
                //        return Result.InternalError("Ошибка при создании выплаты: " + payoutResponse.ErrorMessage);
                //    }
                //
                //    transaction.ProcessingOrderId = payoutResponse.PayoutId;
                //    transaction.PaymentGateway = transaction.Player.PlayerOptions.PayoutMethod.ToString();
                //    transaction.Currency = transaction.Player.PlayerOptions.Currency?.ToString() ?? "rub";
                //    transaction.TransactionStatusId = approvedStatusId;
                //}
                //catch (Exception ex)
                //{
                //    transaction.Player.Balance.Amount += transaction.Amount;
                //    transaction.TransactionStatusId = TransactionStatuses.Rejected;
                //    transaction.IsProcessingSuccess = false;
                //    transaction.ProcessedAt = DateTime.UtcNow;
                //    await _context.SaveChangesAsync();
                //
                //    _logger.LogError(ex, "{Prefix} Ошибка при создании выплаты для игрока {PlayerId}", Prefix, transaction.PlayerId);
                //    return Result.InternalError("Ошибка при создании выплаты");
                //}

                CompleteWithdrawalLocallyForTests(transaction, appBalance, approvedStatusId);
                _logger.LogWarning(
                    "{Prefix} [TEST BYPASS] Вывод {TransactionId} одобрен локально без процессинга",
                    Prefix,
                    transaction.Id);
            }

            transaction.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Prefix} Статус транзакции {TransactionId} изменен на {StatusId}", Prefix, transactionId, request.TransactionStatusId);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<GetPlayerTransactionsResponse>> GetPlayerTransactions(int playerId, int? requestingPlayerId, bool isAdmin)
        {
            var player = await _context.Players
                .Include(p => p.Ban)
                .Include(t => t.GameRounds)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", Prefix, playerId);
                return Result<GetPlayerTransactionsResponse>.NotFound($"Игрок с ID {playerId} не найден");
            }

            if (!isAdmin && requestingPlayerId != playerId)
            {
                return Result<GetPlayerTransactionsResponse>.Unauthorized();
            }

            if (!isAdmin && player.Ban is not null && player.Ban.IsActive)
            {
                _logger.LogWarning("{Prefix} Пользователь забанен", Prefix);
                return Result<GetPlayerTransactionsResponse>.Forbidden("Пользователь в бане");
            }

            var transactions = await _context.MoneyTransactions
                .Include(t => t.TransactionType)
                .Include(t => t.TransactionStatus)
                .Where(t => t.PlayerId == playerId)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("{Prefix} Получено {Count} транзакций для игрока {PlayerId}", Prefix, transactions.Count, playerId);

            var response = new GetPlayerTransactionsResponse
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
                Transactions = [.. transactions.Select(t => new TransactionDTO
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    StatusId = t.TransactionStatusId,
                    StatusName = t.TransactionStatus.Name,
                    TypeId = t.TransactionTypeId,
                    TypeName = t.TransactionType.Name,
                    CreatedAt = t.CreatedAt,
                    ProcessedAt = t.ProcessedAt
                })]
            };

            return Result<GetPlayerTransactionsResponse>.Success(response);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<GetPlayerTransactionsResponse>>> GetAllTransactions(int? typeId)
        {
            var query = _context.MoneyTransactions.AsNoTracking().AsQueryable();

            if (typeId is not null)
            {
                query = query.Where(t => t.TransactionTypeId == typeId);
            }

            var rows = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Amount,
                    t.TransactionStatusId,
                    StatusName = t.TransactionStatus.Name,
                    t.TransactionTypeId,
                    TypeName = t.TransactionType.Name,
                    t.CreatedAt,
                    t.ProcessedAt,
                    t.PlayerId,
                    PlayerTelegramId = t.Player.TelegramId,
                    PlayerName = t.Player.Name,
                    PlayerAvatarUrl = t.Player.AvatarUrl,
                    PlayerRegisteredInAppAt = t.Player.RegisteredInAppAt,
                })
                .ToListAsync();

            _logger.LogInformation("{Prefix} Получено {Count} транзакций", Prefix, rows.Count);

            var response = rows
                .GroupBy(t => t.PlayerId)
                .Select(group =>
                {
                    var player = group.First();
                    return new GetPlayerTransactionsResponse
                    {
                        Player = new PlayerDTO
                        {
                            Id = player.PlayerId,
                            TelegramId = player.PlayerTelegramId,
                            Name = player.PlayerName,
                            AvatarUrl = player.PlayerAvatarUrl,
                            RegisteredInAppAt = player.PlayerRegisteredInAppAt,
                            WinRate = 0,
                        },
                        Transactions = [.. group.Select(t => new TransactionDTO
                        {
                            Id = t.Id,
                            Amount = t.Amount,
                            StatusId = t.TransactionStatusId,
                            StatusName = t.StatusName,
                            TypeId = t.TransactionTypeId,
                            TypeName = t.TypeName,
                            CreatedAt = t.CreatedAt,
                            ProcessedAt = t.ProcessedAt
                        })]
                    };
                })
                .ToList();

            return Result<IEnumerable<GetPlayerTransactionsResponse>>.Success(response);
        }

        #endregion Transaction Methods

        #region Private Test Bypass Methods

        /// <summary>
        /// Локально завершает пополнение без обращения к процессинговой площадке
        /// </summary>
        /// <param name="transaction">Транзакция пополнения</param>
        /// <param name="player">Игрок</param>
        /// <param name="amount">Сумма пополнения</param>
        /// <returns>Ответ API с тестовым идентификатором ордера</returns>
        private async Task<object> CompleteDepositLocallyForTestsAsync(
            MoneyTransaction transaction,
            Player player,
            decimal amount)
        {
            var appBalance = await _context.AppBalance.FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Не найден баланс приложения");

            transaction.TransactionStatusId = TransactionStatuses.Approved;
            transaction.IsProcessingSuccess = true;
            transaction.ProcessedAt = DateTime.UtcNow;
            transaction.ProcessingOrderId = $"test-deposit-{transaction.Id}";
            transaction.PaymentGateway = player.PlayerOptions!.PaymentMethod.ToString();
            transaction.Currency = player.PlayerOptions.Currency?.ToString() ?? "rub";

            player.Balance!.Amount += amount;
            appBalance.Amount += amount;

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "{Prefix} [TEST BYPASS] Пополнение {TransactionId} завершено локально без процессинга",
                Prefix,
                transaction.Id);

            return new
            {
                PaymentLink = (string?)null,
                OrderId = transaction.ProcessingOrderId
            };
        }

        /// <summary>
        /// Локально завершает вывод без обращения к процессинговой площадке
        /// </summary>
        /// <param name="transaction">Транзакция вывода</param>
        /// <param name="appBalance">Баланс приложения</param>
        /// <param name="approvedStatusId">Идентификатор статуса «Одобрено»</param>
        private void CompleteWithdrawalLocallyForTests(
            MoneyTransaction transaction,
            AppBalance appBalance,
            int approvedStatusId)
        {
            transaction.TransactionStatusId = approvedStatusId;
            transaction.IsProcessingSuccess = true;
            transaction.ProcessedAt = DateTime.UtcNow;
            transaction.ProcessingOrderId = $"test-payout-{transaction.Id}";
            transaction.PaymentGateway = transaction.Player!.PlayerOptions!.PayoutMethod.ToString();
            transaction.Currency = transaction.Player.PlayerOptions.Currency?.ToString() ?? "rub";
            appBalance.Amount -= transaction.Amount;
        }

        #endregion Private Test Bypass Methods

        #region Reference Data Methods

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionStatuses()
        {
            var statusesDict = await _cacheService.GetTransactionStatusesAsync();

            var statuses = statusesDict.Select(s => new TransactionStatusOrTypeDTO
            {
                Id = s.Key,
                Name = s.Value
            }).ToList();

            _logger.LogInformation("{Prefix} Выведены статусы транзакций (из кэша)", Prefix);

            return Result<IEnumerable<TransactionStatusOrTypeDTO>>.Success(statuses);
        }

        /// <inheritdoc/>
        public async Task<Result<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionTypes()
        {
            var typesDict = await _cacheService.GetTransactionTypesAsync();

            var types = typesDict.Select(t => new TransactionStatusOrTypeDTO
            {
                Id = t.Key,
                Name = t.Value
            }).ToList();

            _logger.LogInformation("{Prefix} Выведены типы транзакций (из кэша)", Prefix);

            return Result<IEnumerable<TransactionStatusOrTypeDTO>>.Success(types);
        }

        #endregion Reference Data Methods
    }
}




