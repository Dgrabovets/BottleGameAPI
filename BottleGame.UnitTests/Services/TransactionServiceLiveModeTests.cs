using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Domain.Enums;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.External.Processing;
using BottleGame.Infrastructure.Services;
using BottleGame.Infrastructure.Services.Payments;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            _providerMock.Object,
            Options.Create(new ProcessingOptions()));

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
    /// Reads the transaction as it is saved in the database, bypassing the tracked instance
    /// </summary>
    /// <param name="transaction">Transaction</param>
    /// <returns>The saved row</returns>
    private MoneyTransaction Saved(MoneyTransaction transaction) =>
        _context.MoneyTransactions.AsNoTracking().Single(t => t.Id == transaction.Id);

    /// <summary>
    /// Approves the withdrawal while the provider times out, so the payout outcome stays unknown
    /// </summary>
    /// <param name="transaction">Withdrawal transaction</param>
    private async Task ApproveWithUnknownOutcomeAsync(MoneyTransaction transaction)
    {
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("timeout"));

        await _service.UpdateTransactionStatus(transaction.Id, Approve());
    }

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
        Saved(transaction).PayoutState.Should().Be(PayoutState.Refused);
        PlayerBalance(player).Should().Be(1300);
    }

    /// <summary>
    /// A timeout leaves the payout outcome unknown: the withdrawal stays pending in that state and the player is not refunded
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
        var saved = Saved(transaction);
        saved.PayoutState.Should().Be(PayoutState.OutcomeUnknown);
        saved.ProcessingError.Should().BeNull();
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
        Saved(transaction).PayoutState.Should().Be(PayoutState.Completed);
        sent.Should().Equal(transaction.Id, transaction.Id);
        PlayerBalance(player).Should().Be(1000);
    }

    /// <summary>
    /// The unknown outcome, the request time and the destination are saved before the provider is called,
    /// so a crash, a failed final save or a concurrent reject cannot hide a real payout
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_SavesStateAndDestinationBeforeCallingProvider()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        MoneyTransaction? savedAtCall = null;
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => savedAtCall = Saved(transaction))
            .ReturnsAsync(new CreatePayoutResult(true, "payout-1", null));

        var result = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        result.IsSuccess.Should().BeTrue();
        savedAtCall.Should().NotBeNull();
        savedAtCall!.PayoutState.Should().Be(PayoutState.OutcomeUnknown);
        savedAtCall.PayoutRequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        savedAtCall.PaymentGateway.Should().Be("sbp_rub");
        savedAtCall.PayoutWallet.Should().Be("79001234567");
        savedAtCall.Currency.Should().Be("rub");
        savedAtCall.ProcessingError.Should().BeNull();
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
    /// A repeat sends the saved destination, amount and currency with the same transaction id, not the player's current settings
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_Repeat_SendsSavedDestinationNotCurrentSettings()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        await ApproveWithUnknownOutcomeAsync(transaction);
        var options = _context.PlayersOptions.Single(o => o.PlayerId == player.Id);
        options.PayoutMethod = "bank_card";
        options.Wallet = "79009999999:100000000222";
        options.Currency = "kzt";
        _context.SaveChanges();
        CreatePayoutRequest? sent = null;
        _providerMock
            .Setup(x => x.CreatePayoutAsync(It.IsAny<CreatePayoutRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreatePayoutRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(new CreatePayoutResult(true, "payout-1", null));

        var retry = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        retry.IsSuccess.Should().BeTrue();
        sent.Should().NotBeNull();
        sent!.TransactionId.Should().Be(transaction.Id);
        sent.Gateway.Should().Be("sbp_rub");
        sent.Destination.Should().Be("79001234567");
        sent.Currency.Should().Be("rub");
        sent.Amount.Should().Be(300);
        var saved = Saved(transaction);
        saved.PaymentGateway.Should().Be("sbp_rub");
        saved.Currency.Should().Be("rub");
        saved.PayoutWallet.Should().Be("79001234567");
    }

    /// <summary>
    /// Once the idempotence key window has passed since the first request, a repeat could make a second payout,
    /// so it is refused without calling the provider and the withdrawal stays pending without a refund
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_RepeatAfterKeyWindow_IsRefusedWithoutCallingProvider()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        await ApproveWithUnknownOutcomeAsync(transaction);
        var firstRequestedAt = DateTime.UtcNow.AddHours(-25);
        transaction.PayoutRequestedAt = firstRequestedAt;
        _context.SaveChanges();

        var retry = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        retry.IsSuccess.Should().BeFalse();
        retry.StatusCode.Should().Be(409);
        PayoutVerify(Times.Once());
        var saved = Saved(transaction);
        saved.TransactionStatusId.Should().Be(TransactionStatuses.PENDING);
        saved.PayoutState.Should().Be(PayoutState.OutcomeUnknown);
        saved.PayoutRequestedAt.Should().Be(firstRequestedAt);
        PlayerBalance(player).Should().Be(1000);
    }

    /// <summary>
    /// A repeat inside the window is sent and keeps the time of the first request, so repeats never extend the window
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_RepeatInsideKeyWindow_KeepsTheFirstRequestTime()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        await ApproveWithUnknownOutcomeAsync(transaction);
        var firstRequestedAt = DateTime.UtcNow.AddHours(-23);
        transaction.PayoutRequestedAt = firstRequestedAt;
        _context.SaveChanges();

        await _service.UpdateTransactionStatus(transaction.Id, Approve());

        PayoutVerify(Times.Exactly(2));
        Saved(transaction).PayoutRequestedAt.Should().Be(firstRequestedAt);
        Saved(transaction).PayoutState.Should().Be(PayoutState.OutcomeUnknown);
    }

    /// <summary>
    /// A withdrawal converted by the upgrade script has no saved request time or destination, so a repeat is refused without calling the provider
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_RepeatWithoutSavedRequest_IsRefusedWithoutCallingProvider()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        SetPlayedRounds(player, 10);
        var transaction = AddWithdrawal(player, 300);
        transaction.PayoutState = PayoutState.OutcomeUnknown;
        _context.SaveChanges();

        var retry = await _service.UpdateTransactionStatus(transaction.Id, Approve());

        retry.IsSuccess.Should().BeFalse();
        retry.StatusCode.Should().Be(409);
        PayoutVerify(Times.Never());
        Saved(transaction).TransactionStatusId.Should().Be(TransactionStatuses.PENDING);
        Saved(transaction).PayoutState.Should().Be(PayoutState.OutcomeUnknown);
        PlayerBalance(player).Should().Be(1000);
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
        Saved(transaction).PayoutState.Should().Be(PayoutState.OutcomeUnknown);
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
