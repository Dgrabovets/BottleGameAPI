using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.Services;
using BottleGame.Infrastructure.Services.Payments;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Tests for TransactionServiceLiveMode
/// </summary>
public class TransactionServiceLiveModeTests : IDisposable
{
    /// <summary>
    /// Database context of the test
    /// </summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// The service under test
    /// </summary>
    private readonly TransactionService _service;

    /// <summary>
    /// Mock of the payment provider
    /// </summary>
    private readonly Mock<IPaymentProvider> _providerMock = new();

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test
    /// </summary>
    public TransactionServiceLiveModeTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();

        var cacheServiceMock = new Mock<ICacheService>();
        cacheServiceMock.Setup(x => x.GetGameParamsAsync()).ReturnsAsync(_context.GameParams.First());

        var processor = new LiveTransactionProcessor(
            _context,
            new Mock<ILogger<LiveTransactionProcessor>>().Object,
            _providerMock.Object);

        _service = new TransactionService(
            _context,
            new Mock<ILogger<TransactionService>>().Object,
            cacheServiceMock.Object,
            processor);
    }

    /// <summary>
    /// Releases the database context used by the tests
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helpers

    /// <summary>
    /// Adds a pending withdrawal for the player and returns it
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="amount">Amount</param>
    /// <returns>The created value</returns>
    private MoneyTransaction AddWithdrawal(Player player, decimal amount)
    {
        var transaction = new MoneyTransaction
        {
            PlayerId = player.Id,
            Amount = amount,
            TransactionTypeId = TransactionTypes.WITHDRAWAL,
            TransactionStatusId = TransactionStatuses.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        _context.MoneyTransactions.Add(transaction);
        _context.SaveChanges();
        return transaction;
    }

    /// <summary>
    /// Sets how many rounds the player has played since the last deposit
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="count">Count</param>
    private void SetPlayedRounds(Player player, int count)
    {
        _context.PlayersPlayedRoundsStatisticForWithdrawals.Single(s => s.PlayerId == player.Id).PlayedRoundsCount = count;
        _context.SaveChanges();
    }

    /// <summary>
    /// Player balance
    /// </summary>
    /// <param name="player">Player</param>
    /// <returns>The created value</returns>
    private decimal PlayerBalance(Player player) =>
        _context.PlayersBalances.AsNoTracking().Single(b => b.PlayerId == player.Id).Amount;

    /// <summary>
    /// Builds the request that approves a withdrawal
    /// </summary>
    /// <returns>The created value</returns>
    private static ChangeTransactionStatusRequest Approve() =>
        new() { TransactionStatusId = TransactionStatuses.APPROVED };

    /// <summary>
    /// Verifies how many times the provider was asked to create a payout
    /// </summary>
    /// <param name="times">Times</param>
    private void PayoutVerify(Times times) =>
        _providerMock.Verify(
            x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()),
            times);

    #endregion Helpers

    #region Deposit Tests

    /// <summary>
    /// When the provider accepts a deposit, the payment link is returned and the transaction waits for the notification
    /// </summary>
    [Fact]
    public async Task CreateDeposit_ProviderAccepts_ReturnsPaymentLinkAndWaitsForNotification()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _providerMock
            .Setup(x => x.CreatePaymentAsync(It.IsAny<CreatePaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePaymentResult(true, "pay-1", "https://pay.example/1", null));

        var result = await _service.CreateTransaction(player.Id, new MoneyTransactionRequest { IsDeposit = true, Amount = 500 }, player.Id, false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new { PaymentLink = "https://pay.example/1", OrderId = "pay-1" });
        var transaction = _context.MoneyTransactions.Single();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.PENDING);
        transaction.ProcessingOrderId.Should().Be("pay-1");
        transaction.IsProcessingSuccess.Should().BeNull();
        PlayerBalance(player).Should().Be(1000);
    }

    /// <summary>
    /// The payment request sent to the provider carries the transaction id, amount and currency
    /// </summary>
    [Fact]
    public async Task CreateDeposit_RequestCarriesTransactionIdAndAmount()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        CreatePaymentRequest? sent = null;
        _providerMock
            .Setup(x => x.CreatePaymentAsync(It.IsAny<CreatePaymentRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreatePaymentRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new CreatePaymentResult(true, "pay-1", "https://pay.example/1", null));

        await _service.CreateTransaction(player.Id, new MoneyTransactionRequest { IsDeposit = true, Amount = 500 }, player.Id, false);

        sent.Should().NotBeNull();
        sent!.TransactionId.Should().Be(_context.MoneyTransactions.Single().Id);
        sent.Amount.Should().Be(500);
        sent.Currency.Should().Be("rub");
    }

    /// <summary>
    /// When the provider fails, the deposit is rejected and the balance is not credited
    /// </summary>
    [Fact]
    public async Task CreateDeposit_ProviderFails_RejectsTransactionWithoutCreditingBalance()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        _providerMock
            .Setup(x => x.CreatePaymentAsync(It.IsAny<CreatePaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePaymentResult(false, null, null, "gateway down"));

        var result = await _service.CreateTransaction(player.Id, new MoneyTransactionRequest { IsDeposit = true, Amount = 500 }, player.Id, false);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        var transaction = _context.MoneyTransactions.Single();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.REJECTED);
        transaction.ProcessingError.Should().Be("gateway down");
        PlayerBalance(player).Should().Be(1000);
    }

    #endregion Deposit Tests

    #region Withdrawal Tests

    /// <summary>
    /// A withdrawal is refused when the player has not played enough rounds
    /// </summary>
    [Fact]
    public async Task CreateWithdrawal_NotEnoughPlayedRounds_ReturnsFailure()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 1);

        var result = await _service.CreateTransaction(player.Id, new MoneyTransactionRequest { IsDeposit = false, Amount = 300 }, player.Id, false);

        result.IsSuccess.Should().BeFalse();
        _context.MoneyTransactions.Should().BeEmpty();
    }

    /// <summary>
    /// When the provider accepts the payout, the withdrawal is approved and waits for the payout notification
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_ProviderAccepts_WaitsForPayoutNotification()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePayoutResult(true, "payout-1", null));

        var result = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        result.IsSuccess.Should().BeTrue();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.APPROVED);
        transaction.ProcessingOrderId.Should().Be("payout-1");
        transaction.IsProcessingSuccess.Should().BeNull();
        _context.AppBalance.Single().Amount.Should().Be(100000);
    }

    /// <summary>
    /// The payout request carries the player's payout method and wallet
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_RequestCarriesPayoutMethodAndWallet()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var options = _context.PlayersOptions.Single(o => o.PlayerId == player.Id);
        options.PayoutMethod = "sbp";
        options.Wallet = "79001234567:100000000111";
        _context.SaveChanges();
        var transaction = AddWithdrawal(player, 300);
        CreatePayoutRequest? sent = null;
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreatePayoutRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new CreatePayoutResult(true, "payout-1", null));

        await _service.UpdateTransactionStatus(transaction.Id, Approve());

        sent.Should().NotBeNull();
        sent!.TransactionId.Should().Be(transaction.Id);
        sent.Gateway.Should().Be("sbp");
        sent.Destination.Should().Be("79001234567:100000000111");
        sent.Amount.Should().Be(300);
    }

    /// <summary>
    /// When the provider fails, the player is refunded and the withdrawal is rejected
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_ProviderFails_RefundsPlayerAndRejects()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePayoutResult(false, null, "no funds"));

        var result = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.REJECTED);
        transaction.ProcessingError.Should().Be("no funds");
        PlayerBalance(player).Should().Be(1300);
    }

    /// <summary>
    /// A timeout leaves the payout outcome unknown: the withdrawal stays pending, marked, and the player is not refunded
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_ProviderTimesOut_StaysPendingWithoutRefund()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("timeout"));

        var result = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        result.IsSuccess.Should().BeFalse();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.PENDING);
        transaction.IsProcessingSuccess.Should().BeNull();
        transaction.ProcessingError.Should().Be(LiveTransactionProcessor.PAYOUT_OUTCOME_UNKNOWN);
        PlayerBalance(player).Should().Be(1000);
    }

    /// <summary>
    /// Approving again after an unknown outcome sends the same transaction id, so the provider reuses the idempotence key
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_RetryAfterUnknownOutcome_ApprovesWithTheSameTransaction()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        var sent = new List<int>();
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreatePayoutRequest, CancellationToken>((request, _) => sent.Add(request.TransactionId))
            .ThrowsAsync(new HttpRequestException("YooKassa responded 500"));

        await _service.UpdateTransactionStatus(transaction.Id, Approve());
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.PENDING);

        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreatePayoutRequest, CancellationToken>((request, _) => sent.Add(request.TransactionId))
            .ReturnsAsync(new CreatePayoutResult(true, "payout-1", null));

        var retry = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        retry.IsSuccess.Should().BeTrue();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.APPROVED);
        transaction.ProcessingOrderId.Should().Be("payout-1");
        transaction.ProcessingError.Should().BeNull();
        sent.Should().Equal(transaction.Id, transaction.Id);
        PlayerBalance(player).Should().Be(1000);
    }

    /// <summary>
    /// The mark is saved before the provider is called, so a crash, a failed final save or a concurrent reject cannot hide a real payout
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_SavesMarkBeforeCallingProvider()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        string? savedAtCall = "not called";
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => savedAtCall = _context.MoneyTransactions.AsNoTracking().Single(t => t.Id == transaction.Id).ProcessingError)
            .ReturnsAsync(new CreatePayoutResult(true, "payout-1", null));

        var result = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        result.IsSuccess.Should().BeTrue();
        savedAtCall.Should().Be(LiveTransactionProcessor.PAYOUT_OUTCOME_UNKNOWN);
        transaction.ProcessingError.Should().BeNull();
    }

    /// <summary>
    /// A repeat skips the rounds and payout limit checks, which only guard the first request: a deposit in between must not strand the withdrawal
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_Repeat_SkipsRoundsAndLimitChecks()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("timeout"));
        await _service.UpdateTransactionStatus(transaction.Id, Approve());
        SetPlayedRounds(player, 0);
        _context.AppBalance.Single().LowerThreshold = 100;
        _context.SaveChanges();
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePayoutResult(true, "payout-1", null));

        var retry = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        retry.IsSuccess.Should().BeTrue();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.APPROVED);
    }

    /// <summary>
    /// The marker is stored in the database, so its text must never change: rows marked earlier would lose their protection
    /// </summary>
    [Fact]
    public void PayoutOutcomeUnknown_ValueIsStable()
    {
        LiveTransactionProcessor.PAYOUT_OUTCOME_UNKNOWN.Should().Be(
            "Payout outcome unknown: wait for the YooKassa notification or approve again to repeat the request with the same idempotence key");
    }

    /// <summary>
    /// A refused repeat does not prove that the first request made no payout, so the withdrawal stays pending without a refund
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_RetryRefusedAfterUnknownOutcome_StaysPendingWithoutRefund()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("timeout"));
        await _service.UpdateTransactionStatus(transaction.Id, Approve());
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePayoutResult(false, null, "idempotence key reused with other parameters"));

        var retry = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        retry.IsSuccess.Should().BeFalse();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.PENDING);
        transaction.IsProcessingSuccess.Should().BeNull();
        transaction.ProcessingError.Should().Be(LiveTransactionProcessor.PAYOUT_OUTCOME_UNKNOWN);
        PlayerBalance(player).Should().Be(1000);
    }

    /// <summary>
    /// A withdrawal whose payout outcome is unknown cannot be rejected by hand, because the refund could pay twice
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_AfterUnknownOutcome_IsRefusedWithoutRefund()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connection reset"));
        await _service.UpdateTransactionStatus(transaction.Id, Approve());

        var result = await _service.UpdateTransactionStatus(transaction.Id, new ChangeTransactionStatusRequest { TransactionStatusId = TransactionStatuses.REJECTED });

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.PENDING);
        PlayerBalance(player).Should().Be(1000);
    }

    /// <summary>
    /// A withdrawal above the payout limit is refused without calling the provider
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_AmountAboveThreshold_ReturnsFailureWithoutCallingProvider()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 1500); // limit: 10% of the lower threshold of 10000 = 1000

        var result = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        result.IsSuccess.Should().BeFalse();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.PENDING);
        PayoutVerify(Times.Never());
    }

    /// <summary>
    /// A withdrawal is refused without calling the provider when too few rounds were played
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_NotEnoughPlayedRounds_ReturnsFailureWithoutCallingProvider()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 1);
        var transaction = AddWithdrawal(player, 300);

        var result = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        result.IsSuccess.Should().BeFalse();
        PayoutVerify(Times.Never());
    }

    #endregion Withdrawal Tests
}
