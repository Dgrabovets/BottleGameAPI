using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;

namespace BottleGame.UnitTests.Fixtures;

/// <summary>
/// Помощник для подготовки данных статистики в тестах
/// </summary>
public sealed class StatisticsTestDataBuilder
{
    private readonly ApplicationDbContext _context;
    private long _nextTelegramId = 1_000_000_001;

    /// <summary>
    /// Создаёт билдер поверх тестового контекста с справочниками
    /// </summary>
    /// <param name="context">Контекст базы данных</param>
    public StatisticsTestDataBuilder(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Создаёт контекст, билдер и справочные данные одним вызовом
    /// </summary>
    public static (ApplicationDbContext Context, StatisticsTestDataBuilder Builder) Create()
    {
        var context = TestDbContextFactory.CreateWithReferenceData();
        return (context, new StatisticsTestDataBuilder(context));
    }

    /// <summary>
    /// Добавляет игрока с балансом
    /// </summary>
    /// <param name="registeredInAppAt">Дата регистрации</param>
    /// <param name="balance">Текущий баланс</param>
    /// <param name="name">Имя игрока</param>
    public Player AddPlayer(
        DateTime registeredInAppAt,
        decimal balance,
        string name = "StatsPlayer")
    {
        var player = new Player
        {
            TelegramId = _nextTelegramId++,
            Name = name,
            AvatarUrl = "https://example.com/avatar.jpg",
            RegisteredInAppAt = registeredInAppAt
        };

        _context.Players.Add(player);
        _context.SaveChanges();

        _context.PlayersBalances.Add(new Balance
        {
            PlayerId = player.Id,
            Amount = balance
        });

        _context.PlayersOptions.Add(new PlayerOptions
        {
            PlayerId = player.Id,
            WinChance = 0.5f,
            Currency = "rub",
            PaymentMethod = "sbp_rub",
            PayoutMethod = "sbp_rub",
            Wallet = "79001234567"
        });

        _context.PlayersPlayedRoundsStatisticForWithdrawals.Add(
            new PlayerPlayedRoundsStatisticForWithdrawal
            {
                PlayerId = player.Id,
                PlayedRoundsCount = 0
            });

        _context.SaveChanges();
        return player;
    }

    /// <summary>
    /// Добавляет денежную транзакцию с заданным статусом обработки
    /// </summary>
    /// <param name="playerId">Идентификатор игрока</param>
    /// <param name="transactionTypeId">Тип: пополнение или вывод</param>
    /// <param name="transactionStatusId">Статус транзакции</param>
    /// <param name="amount">Сумма</param>
    /// <param name="isProcessingSuccess">Успешность обработки процессингом</param>
    /// <param name="processedAt">Дата обработки</param>
    /// <param name="createdAt">Дата создания</param>
    public MoneyTransaction AddTransaction(
        int playerId,
        int transactionTypeId,
        int transactionStatusId,
        decimal amount,
        bool? isProcessingSuccess = null,
        DateTime? processedAt = null,
        DateTime? createdAt = null)
    {
        var transaction = new MoneyTransaction
        {
            PlayerId = playerId,
            TransactionTypeId = transactionTypeId,
            TransactionStatusId = transactionStatusId,
            Amount = amount,
            IsProcessingSuccess = isProcessingSuccess,
            ProcessedAt = processedAt,
            CreatedAt = createdAt ?? processedAt ?? DateTime.UtcNow
        };

        _context.MoneyTransactions.Add(transaction);
        _context.SaveChanges();
        return transaction;
    }

    /// <summary>
    /// Успешно обработанное пополнение
    /// </summary>
    public MoneyTransaction AddSuccessfulDeposit(
        int playerId,
        decimal amount,
        DateTime processedAt)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.Deposit,
            TransactionStatuses.Approved,
            amount,
            isProcessingSuccess: true,
            processedAt: processedAt,
            createdAt: processedAt.AddHours(-1));
    }

    /// <summary>
    /// Успешно обработанный вывод
    /// </summary>
    public MoneyTransaction AddSuccessfulWithdrawal(
        int playerId,
        decimal amount,
        DateTime processedAt)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.Withdrawal,
            TransactionStatuses.Approved,
            amount,
            isProcessingSuccess: true,
            processedAt: processedAt,
            createdAt: processedAt.AddHours(-1));
    }

    /// <summary>
    /// Вывод в ожидании модерации / обработки
    /// </summary>
    public MoneyTransaction AddPendingWithdrawal(int playerId, decimal amount)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.Withdrawal,
            TransactionStatuses.Pending,
            amount,
            isProcessingSuccess: null,
            processedAt: null);
    }

    /// <summary>
    /// Одобренный, но ещё не подтверждённый процессингом вывод
    /// </summary>
    public MoneyTransaction AddApprovedWithdrawalAwaitingProcessing(int playerId, decimal amount)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.Withdrawal,
            TransactionStatuses.Approved,
            amount,
            isProcessingSuccess: null,
            processedAt: null);
    }

    /// <summary>
    /// Пополнение в ожидании оплаты
    /// </summary>
    public MoneyTransaction AddPendingDeposit(int playerId, decimal amount)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.Deposit,
            TransactionStatuses.Pending,
            amount,
            isProcessingSuccess: null,
            processedAt: null);
    }

    /// <summary>
    /// Неуспешное пополнение после отклонения процессингом
    /// </summary>
    public MoneyTransaction AddFailedDeposit(int playerId, decimal amount, DateTime processedAt)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.Deposit,
            TransactionStatuses.Rejected,
            amount,
            isProcessingSuccess: false,
            processedAt: processedAt);
    }
}
