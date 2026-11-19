using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Services;
using BottleGame.Infrastructure.Services.Payments;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Tests for TransactionService
/// </summary>
public class TransactionServiceTests : IDisposable
{
    /// <summary>
    /// Database context of the test
    /// </summary>
    private readonly Infrastructure.Data.ApplicationDbContext _context;

    /// <summary>
    /// The service under test
    /// </summary>
    private readonly TransactionService _service;

    /// <summary>
    /// Mock of the logger
    /// </summary>
    private readonly Mock<ILogger<TransactionService>> _loggerMock;

    /// <summary>
    /// Mock of the cache service
    /// </summary>
    private readonly Mock<ICacheService> _cacheServiceMock;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test
    /// </summary>
    public TransactionServiceTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();
        _loggerMock = new Mock<ILogger<TransactionService>>();
        _cacheServiceMock = new Mock<ICacheService>();

        var gameParams = _context.GameParams.First();
        _cacheServiceMock.Setup(x => x.GetGameParamsAsync()).ReturnsAsync(gameParams);
        _cacheServiceMock.Setup(x => x.GetTransactionTypesAsync()).ReturnsAsync(
            new Dictionary<int, string>
            {
                { TransactionTypes.Deposit, "Пополнение" },
                { TransactionTypes.Withdrawal, "Вывод" }
            });

        _cacheServiceMock.Setup(x => x.GetTransactionStatusesAsync()).ReturnsAsync(
            new Dictionary<int, string>
            {
                { TransactionStatuses.Pending, "В ожидании" },
                { TransactionStatuses.Approved, "Одобрено" },
                { TransactionStatuses.Rejected, "Отклонено" }
            });

        var processor = new SimulatedTransactionProcessor(_context, new Mock<ILogger<SimulatedTransactionProcessor>>().Object);
        _service = new TransactionService(_context, _loggerMock.Object, _cacheServiceMock.Object, processor);
    }

    /// <summary>
    /// Releases the database context used by the tests
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetPlayerBalance Tests

    /// <summary>
    /// Get player balance: player not found; returns not found
    /// </summary>
    [Fact]
    public async Task GetPlayerBalance_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetPlayerBalance(999, null, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Get player balance: as player; other player, unauthorized
    /// </summary>
    [Fact]
    public async Task GetPlayerBalance_AsPlayer_OtherPlayer_Unauthorized()
    {
        // Arrange
        var player1 = TestDbContextFactory.CreateTestPlayer(_context, 111);
        var player2 = TestDbContextFactory.CreateTestPlayer(_context, 222);

        // Act
        var result = await _service.GetPlayerBalance(player1.Id, player2.Id, false);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    /// <summary>
    /// Get player balance: success; returns balance
    /// </summary>
    [Fact]
    public async Task GetPlayerBalance_Success_ReturnsBalance()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);

        // Act
        var result = await _service.GetPlayerBalance(player.Id, player.Id, false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Balance.Should().Be(1000);
    }

    #endregion GetPlayerBalance Tests

    #region UpdateTransactionStatus Tests

    /// <summary>
    /// Update transaction status: transaction not found; returns not found
    /// </summary>
    [Fact]
    public async Task UpdateTransactionStatus_TransactionNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new ChangeTransactionStatusRequest { TransactionStatusId = TransactionStatuses.Approved };

        // Act
        var result = await _service.UpdateTransactionStatus(999, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Update transaction status: invalid status; returns failure
    /// </summary>
    [Fact]
    public async Task UpdateTransactionStatus_InvalidStatus_ReturnsFailure()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var transaction = new MoneyTransaction
        {
            PlayerId = player.Id,
            Amount = 100,
            TransactionTypeId = TransactionTypes.Withdrawal,
            TransactionStatusId = TransactionStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.MoneyTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        var request = new ChangeTransactionStatusRequest { TransactionStatusId = TransactionStatuses.Pending };

        // Act
        var result = await _service.UpdateTransactionStatus(transaction.Id, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion UpdateTransactionStatus Tests

    #region GetPlayerTransactions Tests

    /// <summary>
    /// Get player transactions: player not found; returns not found
    /// </summary>
    [Fact]
    public async Task GetPlayerTransactions_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetPlayerTransactions(999, null, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Get player transactions: success; returns transactions
    /// </summary>
    [Fact]
    public async Task GetPlayerTransactions_Success_ReturnsTransactions()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _context.MoneyTransactions.Add(new MoneyTransaction
        {
            PlayerId = player.Id,
            Amount = 100,
            TransactionTypeId = TransactionTypes.Deposit,
            TransactionStatusId = TransactionStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPlayerTransactions(player.Id, player.Id, false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Transactions.Should().HaveCount(1);
    }

    #endregion GetPlayerTransactions Tests

    #region GetAllTransactions Tests

    /// <summary>
    /// Get all transactions: returns all transactions
    /// </summary>
    [Fact]
    public async Task GetAllTransactions_ReturnsAllTransactions()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _context.MoneyTransactions.AddRange(
            new MoneyTransaction
            {
                PlayerId = player.Id,
                Amount = 100,
                TransactionTypeId = TransactionTypes.Deposit,
                TransactionStatusId = TransactionStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new MoneyTransaction
            {
                PlayerId = player.Id,
                Amount = 50,
                TransactionTypeId = TransactionTypes.Withdrawal,
                TransactionStatusId = TransactionStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllTransactions(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().NotBeEmpty();
    }

    /// <summary>
    /// Get all transactions: does not duplicate when player has many rounds
    /// </summary>
    [Fact]
    public async Task GetAllTransactions_DoesNotDuplicateWhenPlayerHasManyRounds()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _context.GameRounds.AddRange(
            new GameRound { PlayerId = player.Id, Bet = 100, IsWin = false, FinishedAt = DateTime.UtcNow },
            new GameRound { PlayerId = player.Id, Bet = 100, IsWin = true, FinishedAt = DateTime.UtcNow },
            new GameRound { PlayerId = player.Id, Bet = 50, IsWin = false, FinishedAt = DateTime.UtcNow },
            new GameRound { PlayerId = player.Id, Bet = 50, IsWin = true, FinishedAt = DateTime.UtcNow }
        );

        _context.MoneyTransactions.AddRange(
            new MoneyTransaction
            {
                PlayerId = player.Id,
                Amount = 2000,
                TransactionTypeId = TransactionTypes.Deposit,
                TransactionStatusId = TransactionStatuses.Approved,
                CreatedAt = DateTime.UtcNow
            },
            new MoneyTransaction
            {
                PlayerId = player.Id,
                Amount = 10000,
                TransactionTypeId = TransactionTypes.Withdrawal,
                TransactionStatusId = TransactionStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetAllTransactions(null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value!.Single().Transactions.Should().HaveCount(2);
    }

    /// <summary>
    /// Get all transactions: excludes actively banned player
    /// </summary>
    [Fact]
    public async Task GetAllTransactions_ExcludesActivelyBannedPlayer()
    {
        var active = TestDbContextFactory.CreateTestPlayer(_context, 111);
        var banned = TestDbContextFactory.CreateTestPlayer(_context, 222);
        TestDbContextFactory.AddActiveBan(_context, banned.Id);

        _context.MoneyTransactions.AddRange(
            new MoneyTransaction
            {
                PlayerId = active.Id,
                Amount = 100,
                TransactionTypeId = TransactionTypes.Deposit,
                TransactionStatusId = TransactionStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new MoneyTransaction
            {
                PlayerId = banned.Id,
                Amount = 500,
                TransactionTypeId = TransactionTypes.Deposit,
                TransactionStatusId = TransactionStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetAllTransactions(null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value!.Single().Player.Id.Should().Be(active.Id);
        result.Value!.Single().Transactions.Should().HaveCount(1);
    }

    /// <summary>
    /// Get all transactions: filter by type; returns filtered
    /// </summary>
    [Fact]
    public async Task GetAllTransactions_FilterByType_ReturnsFiltered()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _context.MoneyTransactions.AddRange(
            new MoneyTransaction
            {
                PlayerId = player.Id,
                Amount = 100,
                TransactionTypeId = TransactionTypes.Deposit,
                TransactionStatusId = TransactionStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new MoneyTransaction
            {
                PlayerId = player.Id,
                Amount = 50,
                TransactionTypeId = TransactionTypes.Withdrawal,
                TransactionStatusId = TransactionStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllTransactions(TransactionTypes.Deposit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.All(r => r.Transactions.All(t => t.TypeId == TransactionTypes.Deposit)).Should().BeTrue();
    }

    /// <summary>
    /// A page holds the newest transactions first and the count is the size of the whole list
    /// </summary>
    [Fact]
    public async Task GetAllTransactions_Paged_ReturnsNewestFirstAndCountsAll()
    {
        // Arrange
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        for (var i = 1; i <= 5; i++)
        {
            _context.MoneyTransactions.Add(new MoneyTransaction
            {
                PlayerId = player.Id,
                Amount = i * 100,
                TransactionTypeId = TransactionTypes.Deposit,
                TransactionStatusId = TransactionStatuses.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(i)
            });
        }

        await _context.SaveChangesAsync();

        // Act
        var firstPage = await _service.GetAllTransactions(null, new PageRequest(1, 2));
        var secondPage = await _service.GetAllTransactions(null, new PageRequest(2, 2));
        var total = await _service.CountTransactions(null);

        // Assert
        firstPage.Value!.Single().Transactions.Select(t => t.Amount).Should().Equal(500, 400);
        secondPage.Value!.Single().Transactions.Select(t => t.Amount).Should().Equal(300, 200);
        total.Should().Be(5);
    }

    /// <summary>
    /// The count follows the type filter
    /// </summary>
    [Fact]
    public async Task CountTransactions_FilterByType_CountsOnlyThatType()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _context.MoneyTransactions.AddRange(
            new MoneyTransaction { PlayerId = player.Id, Amount = 1, TransactionTypeId = TransactionTypes.Deposit, TransactionStatusId = TransactionStatuses.Pending, CreatedAt = DateTime.UtcNow },
            new MoneyTransaction { PlayerId = player.Id, Amount = 2, TransactionTypeId = TransactionTypes.Withdrawal, TransactionStatusId = TransactionStatuses.Pending, CreatedAt = DateTime.UtcNow },
            new MoneyTransaction { PlayerId = player.Id, Amount = 3, TransactionTypeId = TransactionTypes.Withdrawal, TransactionStatusId = TransactionStatuses.Pending, CreatedAt = DateTime.UtcNow });

        await _context.SaveChangesAsync();

        (await _service.CountTransactions(TransactionTypes.Withdrawal)).Should().Be(2);
        (await _service.CountTransactions(null)).Should().Be(3);
    }

    #endregion GetAllTransactions Tests

    #region GetAllTransactionStatuses Tests

    /// <summary>
    /// Get all transaction statuses: returns statuses
    /// </summary>
    [Fact]
    public async Task GetAllTransactionStatuses_ReturnsStatuses()
    {
        // Act
        var result = await _service.GetAllTransactionStatuses();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(3);
    }

    #endregion GetAllTransactionStatuses Tests

    #region GetAllTransactionTypes Tests

    /// <summary>
    /// Get all transaction types: returns types
    /// </summary>
    [Fact]
    public async Task GetAllTransactionTypes_ReturnsTypes()
    {
        // Act
        var result = await _service.GetAllTransactionTypes();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
    }

    #endregion GetAllTransactionTypes Tests
}
