using System.Security.Claims;
using BottleGameServer.Data;
using BottleGameServer.Models.DTOs.Requests;
using BottleGameServer.Models.DTOs.Responses;
using BottleGameServer.Models.Entities;
using BottleGameServer.Processing;
using BottleGameServer.Processing.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BottleGameServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BalanceController : Controller
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<PlayerController> _logger;

        /// <inheritdoc cref="IProcessingService"/>"/>
        private IProcessingService _processingService;

        /// <inheritdoc cref="ProcessingOptions"/>
        private readonly ProcessingOptions _options;

        const string prefix = "[Balance Controller]";

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Конструктор контроллера баланса
        /// </summary>
        /// <param name="context">Контекст базы данных</param>
        /// <param name="logger">Логгер</param>
        /// <param name="processingService">Сервис процессинга</param>
        /// <param name="options">Настройки процессинга</param>
        public BalanceController(ApplicationDbContext context, ILogger<PlayerController> logger, IProcessingService processingService, IOptions<ProcessingOptions> options)
        {
            _context = context;
            _logger = logger;
            _processingService = processingService;
            _options = options.Value;
        }

        #endregion Constructor

        #region ActionMethods

        /// <summary>
        /// Получение баланса игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Возвращает текущий баланс игрока</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin,Player")]
        [HttpGet("get-player-balance/{playerId:int}")]
        [ProducesResponseType(typeof(GetPlayerBalanceResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GetPlayerBalanceResponse>> GetPlayerBalance([FromRoute] int playerId)
        {
            try
            {
                if (User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                // Проверка существования игрока
                var player = await _context.Players.Include(p => p.Ban).FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                if (User.IsInRole("Player") && player.Ban is not null && player.Ban.IsActive)
                {
                    _logger.LogWarning("{Prefix} Пользователь забанен", prefix);
                    return Forbid("Пользователь в бане");
                }

                // Получение баланса
                var balance = await _context.PlayersBalances
                    .AsNoTracking()
                    .Where(b => b.PlayerId == playerId)
                    .Select(b => b.Amount)
                    .FirstOrDefaultAsync();

                // Если баланс не найден - выдаем ошибку
                if (balance == default)
                {
                    _logger.LogError("{Prefix} Баланс игрока не найден! Нарушена целостность данных", prefix);
                }

                _logger.LogInformation("{Prefix} Успешно получен баланс для игрока {PlayerId}: {Amount}", prefix, playerId, balance);

                return Ok(new GetPlayerBalanceResponse { PlayerId = playerId, Balance = balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка получения баланса для игрока {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Создать транзакцию
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="request">Запрос для создания транзакции</param>
        /// <response code="200">Успешное создание транзакции</response>
        /// <response code="400">Некорректный запрос/Недостаточно средств</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin,Player")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpPost("create-transaction/{playerId:int}")]
        public async Task<ActionResult> CreateTransaction([FromRoute] int playerId, [FromBody] MoneyTransactionRequest request)
        {
            try
            {
                // Проверка валидности модели запроса
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные запроса для игрока {PlayerId}", prefix, playerId);
                    return BadRequest(ModelState);
                }

                // Проверка, что игрок может совершать операцию только со своим аккаунтом
                if (User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                // Поиск игрока с балансом и баном
                var player = await _context.Players
                    .Include(p => p.PlayerOptions)
                    .Include(p => p.Balance)
                    .Include(p => p.Ban)
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                // Если игрок не найден — возвращаем 404
                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                // Если игрок забанен — возвращаем 403
                if (User.IsInRole("Player") && player.Ban is not null && player.Ban.IsActive)
                {
                    _logger.LogWarning("{Prefix} Пользователь забанен", prefix);
                    return Forbid("Пользователь в бане");
                }

                // Если это не депозит и средств недостаточно — возвращаем 400
                if (!request.IsDeposit && player.Balance.Amount < request.Amount)
                {
                    _logger.LogWarning("{Prefix} Недостаточно средств на балансе для снятия у игрока {PlayerId}", prefix, playerId);
                    return BadRequest("Недостаточно средств!");
                }

                // Получаем игровые параметры (например, минимальная сумма депозита/вывода)
                var gameParams = await _context.GameParams.FirstOrDefaultAsync();

                if (gameParams is null)
                {
                    _logger.LogWarning("{Prefix} Не найдены игровые параметры", prefix);
                    return BadRequest("Не найдены игровые параметры");
                }

                // Получаем статистику сыгранных раундов для вывода
                var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                            .FirstOrDefaultAsync(s => s.PlayerId == playerId);

                if (roundsPlayedStat is null)
                {
                    _logger.LogWarning("{Prefix} Не найдены данные статистики сыгранных раундов для игрока {PlayerId}", prefix, playerId);
                    return BadRequest("Не найдены данные статистики сыгранных раундов игрока");
                }

                // Если это не депозит и сыграно недостаточно раундов — возвращаем 400
                if (!request.IsDeposit && roundsPlayedStat.PlayedRoundsCount < gameParams.MinRoundsCountForWithdrawal)
                {
                    _logger.LogWarning("{Prefix} Недостаточно сыгранных раундов для вывода у игрока {PlayerId}", prefix, playerId);
                    return BadRequest($"Для вывода необходимо сыграть минимум {gameParams.MinRoundsCountForWithdrawal} раундов. Сейчас сыграно: {roundsPlayedStat.PlayedRoundsCount}");
                }

                // Получаем тип и статус транзакции (1 - депозит, 2 - вывод, 1 - статус "Ожидание")
                var transactionType = await _context.TransactionTypes.FirstOrDefaultAsync(t => t.Id == (request.IsDeposit ? 1 : 2));
                var transactionStatus = await _context.TransactionStatuses.FirstOrDefaultAsync(s => s.Id == 1);

                // Создаем новую транзакцию
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

                // Добавляем транзакцию в контекст
                _context.MoneyTransactions.Add(transaction);

                if (!request.IsDeposit)
                {
                    // Обновляем баланс игрока (уменьшаем при выводе)
                    player.Balance.Amount -= request.Amount;
                }

                // Сохраняем все изменения в базе данных
                await _context.SaveChangesAsync();

                CreatePaymentResponse depositResponse = null!;

                if (request.IsDeposit)
                {
                    try
                    {
                        _logger.LogInformation("{Prefix} Создание запроса для платежа игрока {PlayerId}, сумма {Amount}", prefix, playerId, request.Amount);

                        var createPaymentRequest = new CreatePaymentRequest(merchant_id: _options.MerchantId,
                                                                            secret: _options.SecretKey,
                                                                            order_id: transaction.Id.ToString(),
                                                                            customer: player.Name,
                                                                            amount: request.Amount * 100,
                                                                            currency: player.PlayerOptions.Currency.ToString(),
                                                                            method: player.PlayerOptions.PaymentMethod.ToString());

                        var paymentResponse = await _processingService.CreatePaymentAsync(createPaymentRequest);

                        if (!paymentResponse.IsSuccess || paymentResponse.Payment is null)
                        {
                            transaction.TransactionStatusId = 3;
                            transaction.IsProcessingSuccess = false;
                            _logger.LogError("{Prefix} Ошибка при создании платежа для игрока {PlayerId}: {ErrorMessage}", prefix, playerId, paymentResponse.ErrorMessage);
                            return StatusCode(500, "Ошибка при создании платежа: " + paymentResponse.ErrorMessage);
                        }

                        depositResponse = paymentResponse.Payment!;
                    }
                    catch (Exception ex)
                    {
                        transaction.TransactionStatusId = 3;
                        transaction.IsProcessingSuccess = false;
                        _logger.LogError(ex, "{Prefix} Ошибка при создании платежа для игрока {PlayerId}", prefix, playerId);
                        return StatusCode(500, "Ошибка при создании платежа");
                    }
                    finally
                    {
                        // Сохраняем все изменения в базе данных
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation(
                    "{Prefix} Транзакция создана для игрока {PlayerId}. Тип: {Status}, Сумма: {Bet}",
                    prefix,
                    playerId,
                    request.IsDeposit ? "Пополнение" : "Вывод",
                    request.Amount);

                if (depositResponse is not null)
                {
                    return Ok(new { PaymentLink = depositResponse.link, ExpiredDate = DateTimeOffset.FromUnixTimeSeconds(depositResponse.expired).LocalDateTime});
                }

                return Ok();
            }
            catch (Exception ex)
            {
                // Логируем и возвращаем 500 при любой ошибке
                _logger.LogError(ex, "{Prefix} Ошибка при создании транзакции для игрока {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Изменить статус транзакции (пополнение или вывод).
        /// </summary>
        /// <param name="transactionId">ID транзакции</param>
        /// <param name="request">Модель запроса: Новый статус (2 - Одобрено, 3 - Отклонено)</param>
        /// <response code="200">Статус транзакции успешно изменен</response>
        /// <response code="400">Некорректный запрос/Неверный статус</response>
        /// <response code="404">Транзакция не найдена</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpPost("update-transaction-status/{transactionId:int}")]
        public async Task<ActionResult> UpdateTransactionStatus([FromRoute] int transactionId, [FromBody] ChangeTransactionStatusRequest request)
        {
            try
            {
                var statuses = await _context.TransactionStatuses.ToListAsync();
                var types = await _context.TransactionTypes.ToListAsync();

                var inWaitStatusId = statuses.First(s => s.Name == "Ожидание").Id;
                var rejectedStatusId = statuses.First(s => s.Name == "Отклонено").Id;
                var approvedStatusId = statuses.First(s => s.Name == "Одобрено").Id;

                var depositTypeId = types.First(t => t.Name == "Депозит" || t.Name == "Пополнение").Id;
                var withdrawalTypeId = types.First(t => t.Name == "Вывод").Id;

                // Проверка допустимости статуса
                if (request.TransactionStatusId == inWaitStatusId)
                {
                    _logger.LogWarning("{Prefix} Некорректный статус транзакции: {StatusId}", prefix, request.TransactionStatusId);
                    return BadRequest($"Допустимые статусы: {approvedStatusId}-Одобрено, {rejectedStatusId}-Отклонено");
                }

                // Поиск транзакции с игроком и балансом
                var transaction = await _context.MoneyTransactions
                    .Include(t => t.Player)
                        .ThenInclude(p => p.PlayerOptions)
                    .Include(t => t.Player)
                        .ThenInclude(p => p.Balance)
                    .FirstOrDefaultAsync(t => t.Id == transactionId);

                if (transaction is null)
                {
                    _logger.LogWarning("{Prefix} Транзакция с ID {TransactionId} не найдена", prefix, transactionId);
                    return NotFound($"Транзакция с ID {transactionId} не найдена");
                }

                if (transaction.Player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок транзакции {transaction} не найден", prefix, transaction.Id);
                    return NotFound($"Игрок транзакции {transaction.Id} не найден");
                }

                if (transaction.Player!.Balance is null)
                {
                    _logger.LogWarning("{Prefix} Не найден баланс игрока транзакции {transaction}", prefix, transaction.Id);
                    return NotFound($"Баланс игрока транзакции {transaction.Id} не найден");
                }

                if (transaction.Player.PlayerOptions is null)
                {
                    _logger.LogWarning("{Prefix} Настройки игрока транзакции {transaction} не найдены", prefix, transaction.Id);
                    return NotFound($"Настройки игрока транзакции {transaction.Id} не найдены");
                }

                // Проверка типа транзакции: только вывод
                if (transaction.TransactionTypeId != withdrawalTypeId)
                {
                    _logger.LogWarning("{Prefix} Тип транзакции {TransactionId} не поддерживается для ручного одобрения/отклонения: {TransactionType}", prefix, transactionId, transaction.TransactionTypeId);
                    return BadRequest("Возможно изменять статус только у транзакций с типом \"Вывод\"");
                }

                // Проверка что транзакция еще не обработана
                if (transaction.TransactionStatusId != inWaitStatusId)
                {
                    _logger.LogWarning("{Prefix} Транзакция {TransactionId} уже обработана (статус {StatusId})",
                        prefix, transactionId, transaction.TransactionStatusId);
                    return BadRequest("Нельзя изменить статус уже обработанной транзакции");
                }

                // Проверка текущего статуса
                if (transaction.TransactionStatusId == request.TransactionStatusId)
                {
                    _logger.LogWarning("{Prefix} Транзакция {TransactionId} уже имеет статус {StatusId}", prefix, transactionId, request.TransactionStatusId);
                    return BadRequest("Транзакция уже имеет указанный статус");
                }

                // Получаем игровые параметры (для вывода)
                var gameParams = await _context.GameParams.FirstOrDefaultAsync();
                if (gameParams is null)
                {
                    _logger.LogWarning("{Prefix} Не найдены игровые параметры", prefix);
                    return BadRequest("Не найдены игровые параметры");
                }

                // Получаем статистику сыгранных раундов для вывода (для вывода)
                var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                    .FirstOrDefaultAsync(s => s.PlayerId == transaction.PlayerId);

                if (roundsPlayedStat is null)
                {
                    _logger.LogWarning("{Prefix} Не найдены данные статистики сыгранных раундов для игрока {PlayerId}", prefix, transaction.PlayerId);
                    return BadRequest("Не найдены данные статистики сыгранных раундов игрока");
                }

                if (transaction.TransactionTypeId == withdrawalTypeId)
                {
                    // Проверка: достаточно ли сыграно раундов для вывода (только при одобрении)
                    if (request.TransactionStatusId == approvedStatusId)
                    {
                        if (roundsPlayedStat.PlayedRoundsCount < gameParams.MinRoundsCountForWithdrawal)
                        {
                            _logger.LogWarning("{Prefix} Недостаточно сыгранных раундов для вывода у игрока {PlayerId}", prefix, transaction.PlayerId);
                            return BadRequest($"Для вывода необходимо сыграть минимум {gameParams.MinRoundsCountForWithdrawal} раундов. Сейчас сыграно: {roundsPlayedStat.PlayedRoundsCount}");
                        }
                    }
                }

                // Получаем баланс приложения
                var appBalance = await _context.AppBalance.FirstOrDefaultAsync();
                if (appBalance is null)
                {
                    _logger.LogWarning("{Prefix} Не найдены данные баланса приложения", prefix);
                    return BadRequest("Не найдены данные баланса приложения");
                }

                if (transaction.TransactionTypeId == withdrawalTypeId)
                {
                    // Вывод
                    if (request.TransactionStatusId == rejectedStatusId)
                    {
                        // Отклонено: возвращаем деньги игроку
                        transaction.Player.Balance.Amount += transaction.Amount;
                        transaction.TransactionStatusId = request.TransactionStatusId;
                    }
                    else if (request.TransactionStatusId == approvedStatusId)
                    {
                        // Одобрено: списываем с баланса приложения
                        if (transaction.Amount > appBalance.LowerThreshold * 0.1)
                        {
                            _logger.LogWarning("{Prefix} Одобрение транзакции невозможно - сумма к выводу превышает нижний порог баланса приложения!", prefix);
                            return BadRequest("Одобрение транзакции невозможно - сумма к выводу превышает нижний порог баланса приложения!");
                        }

                        try
                        {
                            _logger.LogInformation("{Prefix} Создание payout-запроса для игрока {PlayerId}, сумма {Amount}", prefix, transaction.PlayerId, transaction.Amount);

                            var createPayoutRequest = new CreatePayoutRequest(
                                merchant_id: _options.MerchantId,
                                secret: _options.SecretKey,
                                order_id: transaction.Id.ToString(),
                                balance: CurrencyType.USDT.ToString(),
                                method: transaction.Player.PlayerOptions.PayoutMethod.ToString(),
                                wallet: transaction.Player.PlayerOptions.Wallet,
                                amount: transaction.Amount / 80 * 100,
                                comment: null,
                                fee_merchant: false
                            );

                            var payoutResponse = await _processingService.CreatePayoutAsync(createPayoutRequest);

                            if (!payoutResponse.IsSuccess || payoutResponse.Payout is null)
                            {
                                transaction.Player.Balance.Amount += transaction.Amount;
                                transaction.TransactionStatusId = 3;
                                transaction.IsProcessingSuccess = false;
                                _logger.LogError("{Prefix} Ошибка при создании выплаты для игрока {PlayerId}: {ErrorMessage}", prefix, transaction.PlayerId, payoutResponse.ErrorMessage);
                                return StatusCode(500, "Ошибка при создании выплаты: " + payoutResponse.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Player.Balance.Amount += transaction.Amount;
                            transaction.TransactionStatusId = 3;
                            transaction.IsProcessingSuccess = false;
                            _logger.LogError(ex, "{Prefix} Ошибка при создании выплаты для игрока {PlayerId}", prefix, transaction.PlayerId);
                            return StatusCode(500, "Ошибка при создании выплаты");
                        }
                        finally
                        {
                            transaction.ProcessedAt = DateTime.UtcNow;
                            // Сохраняем все изменения в базе данных
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                // Обновляем время обработки транзакции
                transaction.ProcessedAt = DateTime.UtcNow;

                // Сохраняем изменения в базе данных
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "{Prefix} Статус транзакции {TransactionId} изменен на {StatusName}",
                    prefix,
                    transactionId,
                    request.TransactionStatusId == approvedStatusId ? approvedStatusId : rejectedStatusId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при изменении статуса транзакции {TransactionId}", prefix, transactionId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить все транзакции пользователя
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Список транзакций пользователя</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin, Player")]
        [HttpGet("get-player-transactions/{playerId:int}")]
        [ProducesResponseType(typeof(GetPlayerTransactionsResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GetPlayerTransactionsResponse>> GetPlayerTransactions([FromRoute] int playerId)
        {
            try
            {
                // Проверка существования игрока
                var player = await _context.Players.Include(p => p.Ban).Include(t => t.GameRounds).FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                // Проверка, что игрок может совершать операцию только со своим аккаунтом
                if (User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                // Если игрок забанен — возвращаем 403
                if (User.IsInRole("Player") && player.Ban is not null && player.Ban.IsActive)
                {
                    _logger.LogWarning("{Prefix} Пользователь забанен", prefix);
                    return Forbid("Пользователь в бане");
                }

                // Получение транзакций
                var transactions = await _context.MoneyTransactions
                    .Include(t => t.TransactionType)
                    .Include(t => t.TransactionStatus)
                    .Where(t => t.PlayerId == playerId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("{Prefix} Получено {Count} транзакций для игрока {PlayerId}", prefix, transactions.Count, playerId);

                var response = new GetPlayerTransactionsResponse
                {
                    Player = new PlayerDTO
                    {
                        Id = playerId,
                        TelegramId = player!.TelegramId,
                        Name = player.Name,
                        AvatarUrl = player.AvatarUrl,
                        RegisteredInAppAt = player.RegisteredInAppAt,
                        WinRate = player.WinRate,
                    },
                    Transactions = transactions.Select(t => new TransactionDTO
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        StatusId = t.TransactionStatusId,
                        StatusName = t.TransactionStatus.Name,
                        TypeId = t.TransactionTypeId,
                        TypeName = t.TransactionType.Name,
                        CreatedAt = t.CreatedAt,
                        ProcessedAt = t.ProcessedAt
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении транзакций игрока {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить все выводы
        /// </summary>
        /// <param name="typeId">Тип транзакции</param>
        /// <response code="200">Список всех транзакций по указанному типу</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [HttpGet("get-all-transactions")]
        [ProducesResponseType(typeof(IEnumerable<GetPlayerTransactionsResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<GetPlayerTransactionsResponse>>> GetAllTransactions([FromQuery] int? typeId)
        {
            try
            {
                // Получение транзакций
                // Базовый запрос (без фильтрации)
                var query = _context.MoneyTransactions
                    .Include(t => t.Player)
                        .ThenInclude(p => p.GameRounds)
                    .Include(t => t.TransactionType)
                    .Include(t => t.TransactionStatus)
                    .AsQueryable(); // Явное указание типа

                // Фильтрация (если typeId указан)
                if (typeId != null)
                {
                    query = query.Where(t => t.TransactionTypeId == typeId);
                }

                // Сортировка и выполнение
                var transactions = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                if (typeId is not null)
                {
                    transactions = transactions.Where(t => t.TransactionTypeId == typeId).ToList();
                }

                _logger.LogInformation("{Prefix} Получено {Count} транзакций", prefix, transactions.Count());

                var players = transactions.Select(t => t.Player).Distinct().ToList();

                var response = players.Select(p => new GetPlayerTransactionsResponse
                {
                    Player = new PlayerDTO
                    {
                        Id = p.Id,
                        TelegramId = p.TelegramId,
                        Name = p.Name,
                        AvatarUrl = p.AvatarUrl,
                        RegisteredInAppAt = p.RegisteredInAppAt,
                        WinRate = p.WinRate,
                    },
                    Transactions = transactions.Where(t => t.PlayerId == p.Id).Select(t => new TransactionDTO
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        StatusId = t.TransactionStatusId,
                        StatusName = t.TransactionStatus.Name,
                        TypeId = t.TransactionTypeId,
                        TypeName = t.TransactionType.Name,
                        CreatedAt = t.CreatedAt,
                        ProcessedAt = t.ProcessedAt
                    }).ToList()
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении транзакций", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить все доступные статусы транзакций
        /// </summary>
        /// <response code="200">Список всех статусов транзакций</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [HttpGet("get-all-transaction-statuses")]
        [ProducesResponseType(typeof(IEnumerable<TransactionStatusOrTypeDTO>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionStatuses()
        {
            try
            {
                var statuses = await _context.TransactionStatuses
                    .AsNoTracking()
                    .Select(s => new TransactionStatusOrTypeDTO
                    {
                        Id = s.Id,
                        Name = s.Name
                    })
                    .ToListAsync();

                _logger.LogInformation("{Prefix} Выведены статусы транзакций", prefix);

                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении статусов транзакций", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить все возможные типы транзакций
        /// </summary>
        /// <response code="200">Список всех типов транзакций</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [HttpGet("get-all-transaction-types")]
        [ProducesResponseType(typeof(IEnumerable<TransactionStatusOrTypeDTO>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionTypes()
        {
            try
            {
                var types = await _context.TransactionTypes
                    .AsNoTracking()
                    .Select(s => new TransactionStatusOrTypeDTO
                    {
                        Id = s.Id,
                        Name = s.Name
                    })
                    .ToListAsync();

                _logger.LogInformation("{Prefix} Выведены типы транзакций", prefix);

                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении типов транзакций", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        #endregion ActionMethods
    }
}
