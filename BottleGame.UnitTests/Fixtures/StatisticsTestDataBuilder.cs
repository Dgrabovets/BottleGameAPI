using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;

namespace BottleGame.UnitTests.Fixtures;

/// <summary>
/// Helper for preparing statistics data in tests
/// </summary>
public sealed class StatisticsTestDataBuilder
{
    /// <summary>
    /// Database context of the test
    /// </summary>
    private readonly ApplicationDbContext _context;
    /// <summary>
    /// Next telegram id
    /// </summary>
    private long _nextTelegramId = 1_000_000_001;

    /// <summary>
    /// Creates a builder on top of a test context with reference data
    /// </summary>
    /// <param name="context">Database context</param>
    public StatisticsTestDataBuilder(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates the context, the builder and the reference data in a single call
    /// </summary>
    public static (ApplicationDbContext Context, StatisticsTestDataBuilder Builder) Create()
    {
        var context = TestDbContextFactory.CreateWithReferenceData();
        return (context, new StatisticsTestDataBuilder(context));
    }

    /// <summary>
    /// Adds a player with a balance
    /// </summary>
    /// <param name="registeredInAppAt">Registration date</param>
    /// <param name="balance">Current balance</param>
    /// <param name="name">Player name</param>
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
    /// Adds a money transaction with the given processing status
    /// </summary>
    /// <param name="playerId">Player identifier</param>
    /// <param name="transactionTypeId">Type: deposit or withdrawal</param>
    /// <param name="transactionStatusId">Transaction status</param>
    /// <param name="amount">Amount</param>
    /// <param name="isProcessingSuccess">Whether the processing succeeded</param>
    /// <param name="processedAt">Processing date</param>
    /// <param name="createdAt">Creation date</param>
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
    /// Successfully processed deposit
    /// </summary>
    public MoneyTransaction AddSuccessfulDeposit(
        int playerId,
        decimal amount,
        DateTime processedAt)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.DEPOSIT,
            TransactionStatuses.APPROVED,
            amount,
            isProcessingSuccess: true,
            processedAt: processedAt,
            createdAt: processedAt.AddHours(-1));
    }

    /// <summary>
    /// Successfully processed withdrawal
    /// </summary>
    public MoneyTransaction AddSuccessfulWithdrawal(
        int playerId,
        decimal amount,
        DateTime processedAt)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.WITHDRAWAL,
            TransactionStatuses.APPROVED,
            amount,
            isProcessingSuccess: true,
            processedAt: processedAt,
            createdAt: processedAt.AddHours(-1));
    }

    /// <summary>
    /// Withdrawal waiting for moderation / processing
    /// </summary>
    public MoneyTransaction AddPendingWithdrawal(int playerId, decimal amount)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.WITHDRAWAL,
            TransactionStatuses.PENDING,
            amount,
            isProcessingSuccess: null,
            processedAt: null);
    }

    /// <summary>
    /// Withdrawal that is approved but not yet confirmed by the processing
    /// </summary>
    public MoneyTransaction AddApprovedWithdrawalAwaitingProcessing(int playerId, decimal amount)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.WITHDRAWAL,
            TransactionStatuses.APPROVED,
            amount,
            isProcessingSuccess: null,
            processedAt: null);
    }

    /// <summary>
    /// Deposit waiting for payment
    /// </summary>
    public MoneyTransaction AddPendingDeposit(int playerId, decimal amount)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.DEPOSIT,
            TransactionStatuses.PENDING,
            amount,
            isProcessingSuccess: null,
            processedAt: null);
    }

    /// <summary>
    /// Failed deposit after a rejection by the processing
    /// </summary>
    public MoneyTransaction AddFailedDeposit(int playerId, decimal amount, DateTime processedAt)
    {
        return AddTransaction(
            playerId,
            TransactionTypes.DEPOSIT,
            TransactionStatuses.REJECTED,
            amount,
            isProcessingSuccess: false,
            processedAt: processedAt);
    }
}
