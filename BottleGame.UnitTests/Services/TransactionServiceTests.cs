using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.External.Processing;
using BottleGame.Infrastructure.Services;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Тесты для TransactionService
/// </summary>
public class TransactionServiceTests : IDisposable
{
    private readonly Infrastructure.Data.ApplicationDbContext _context;
    private readonly TransactionService _service;
    private readonly Mock<ILogger<TransactionService>> _loggerMock;
    private readonly Mock<IProcessingService> _processingServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;

    public TransactionServiceTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();
        _loggerMock = new Mock<ILogger<TransactionService>>();
        _processingServiceMock = new Mock<IProcessingService>();
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

        var options = Options.Create(new ProcessingOptions
        {
            MerchantId = "test_merchant",
            AccessToken = "test_access_token",
            BaseUrl = "https://app.payshark.net",
            CallbackUrl = "https://test.com/api/webhook"
        });

        _service = new TransactionService(_context, _loggerMock.Object, _processingServiceMock.Object, _cacheServiceMock.Object, options);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetPlayerBalance Tests

    [Fact]
    public async Task GetPlayerBalance_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetPlayerBalance(999, null, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

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

    [Fact]
    public async Task GetPlayerTransactions_PlayerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetPlayerTransactions(999, null, true);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

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

    #endregion GetAllTransactions Tests

    #region GetAllTransactionStatuses Tests

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
