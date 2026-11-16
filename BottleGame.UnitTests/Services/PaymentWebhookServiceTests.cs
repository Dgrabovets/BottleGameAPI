using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.Services;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Tests for PaymentWebhookService.
/// </summary>
public class PaymentWebhookServiceTests : IDisposable
{
    /// <summary>
    /// Payment id.
    /// </summary>
    private const string PaymentId = "pay-1";

    /// <summary>
    /// Payout id.
    /// </summary>
    private const string PayoutId = "payout-1";

    /// <summary>
    /// Database context of the test.
    /// </summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Mock of the payment provider.
    /// </summary>
    private readonly Mock<IPaymentProvider> _providerMock = new();

    /// <summary>
    /// The service under test.
    /// </summary>
    private readonly PaymentWebhookService _service;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test.
    /// </summary>
    public PaymentWebhookServiceTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();
        _service = new PaymentWebhookService(_context, _providerMock.Object, new Mock<ILogger<PaymentWebhookService>>().Object);
    }

    /// <summary>
    /// Releases the database context used by the tests.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helpers

    /// <summary>
    /// Adds a pending transaction for the player and returns it.
    /// </summary>
    /// <param name="player">Player.</param>
    /// <param name="typeId">Type id.</param>
    /// <param name="amount">Amount.</param>
    /// <returns>The created value.</returns>
    private MoneyTransaction AddTransaction(Player player, int typeId, decimal amount)
    {
        var transaction = new MoneyTransaction
        {
            PlayerId = player.Id,
            Amount = amount,
            TransactionTypeId = typeId,
            TransactionStatusId = TransactionStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.MoneyTransactions.Add(transaction);
        _context.SaveChanges();
        return transaction;
    }

    /// <summary>
    /// Builds a notification request for the given event and object id.
    /// </summary>
    /// <param name="event">The event name.</param>
    /// <param name="objectId">Object id.</param>
    /// <returns>The created value.</returns>
    private static WebhookRequestInfo Notification(string @event, string objectId) =>
        new("{\"type\":\"notification\",\"event\":\"" + @event + "\",\"object\":{\"id\":\"" + objectId + "\",\"status\":\"ignored-by-service\"}}",
            "POST", "application/json", "127.0.0.1", "tests");

    /// <summary>
    /// Makes the provider report the given status for the payment.
    /// </summary>
    /// <param name="transaction">Transaction.</param>
    /// <param name="status">Status.</param>
    /// <param name="amount">Amount.</param>
    /// <param name="transactionId">Transaction id.</param>
    private void ProviderReportsPayment(MoneyTransaction transaction, ProviderOperationStatus status, decimal? amount = null, int? transactionId = null) =>
        _providerMock
            .Setup(x => x.GetPaymentAsync(PaymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderOperation(PaymentId, status, amount ?? transaction.Amount, "rub", transactionId ?? transaction.Id, null));

    /// <summary>
    /// Makes the provider report the given status for the payout.
    /// </summary>
    /// <param name="transaction">Transaction.</param>
    /// <param name="status">Status.</param>
    /// <param name="reason">Reason.</param>
    private void ProviderReportsPayout(MoneyTransaction transaction, ProviderOperationStatus status, string? reason = null) =>
        _providerMock
            .Setup(x => x.GetPayoutAsync(PayoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderOperation(PayoutId, status, transaction.Amount, "rub", transaction.Id, reason));

    /// <summary>
    /// Player balance.
    /// </summary>
    /// <param name="player">Player.</param>
    /// <returns>The created value.</returns>
    private decimal PlayerBalance(Player player) =>
        _context.PlayersBalances.AsNoTracking().Single(b => b.PlayerId == player.Id).Amount;

    /// <summary>
    /// App balance.
    /// </summary>
    /// <returns>The created value.</returns>
    private decimal AppBalance() => _context.AppBalance.AsNoTracking().Single().Amount;

    /// <summary>
    /// Links the invited player to the inviter.
    /// </summary>
    /// <param name="invited">Invited.</param>
    /// <param name="inviter">Inviter.</param>
    private void AddInvitation(Player invited, Player inviter)
    {
        _context.InvitedPlayers.Add(new InvitedPlayer
        {
            PlayerIdInvited = invited.Id,
            PlayerIdInvitedBy = inviter.Id,
            InvitedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    #endregion Helpers

    #region Payment Tests

    /// <summary>
    /// A successful payment credits the player and the application balance and approves the transaction.
    /// </summary>
    [Fact]
    public async Task PaymentSucceeded_CreditsPlayerAndAppBalance()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var transaction = AddTransaction(player, TransactionTypes.Deposit, 500);
        ProviderReportsPayment(transaction, ProviderOperationStatus.Succeeded);

        var result = await _service.HandleNotificationAsync(Notification("payment.succeeded", PaymentId));

        result.IsSuccess.Should().BeTrue();
        PlayerBalance(player).Should().Be(1500);
        AppBalance().Should().Be(100500);
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.Approved);
        transaction.IsProcessingSuccess.Should().BeTrue();
        transaction.ProcessingOrderId.Should().Be(PaymentId);
    }

    /// <summary>
    /// Delivering the same successful notification twice credits the balance once; the second delivery is logged as a duplicate.
    /// </summary>
    [Fact]
    public async Task PaymentSucceeded_DeliveredTwice_CreditsOnlyOnce()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var transaction = AddTransaction(player, TransactionTypes.Deposit, 500);
        ProviderReportsPayment(transaction, ProviderOperationStatus.Succeeded);
        var request = Notification("payment.succeeded", PaymentId);

        await _service.HandleNotificationAsync(request);
        var second = await _service.HandleNotificationAsync(request);

        second.IsSuccess.Should().BeTrue();
        PlayerBalance(player).Should().Be(1500);
        AppBalance().Should().Be(100500);
        _context.ProcessingWebhookLogs.Select(l => l.ProcessingStatus)
            .Should().BeEquivalentTo(["Success", "Duplicate"]);
    }

    /// <summary>
    /// A canceled payment rejects the transaction and leaves both balances unchanged.
    /// </summary>
    [Fact]
    public async Task PaymentCanceled_RejectsTransactionWithoutChangingBalances()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var transaction = AddTransaction(player, TransactionTypes.Deposit, 500);
        ProviderReportsPayment(transaction, ProviderOperationStatus.Canceled);

        var result = await _service.HandleNotificationAsync(Notification("payment.canceled", PaymentId));

        result.IsSuccess.Should().BeTrue();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.Rejected);
        PlayerBalance(player).Should().Be(1000);
        AppBalance().Should().Be(100000);
    }

    /// <summary>
    /// A payment that is still pending at the provider leaves the transaction as it is.
    /// </summary>
    [Fact]
    public async Task PaymentStillPending_LeavesTransactionUntouched()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var transaction = AddTransaction(player, TransactionTypes.Deposit, 500);
        ProviderReportsPayment(transaction, ProviderOperationStatus.Pending);

        var result = await _service.HandleNotificationAsync(Notification("payment.succeeded", PaymentId));

        result.IsSuccess.Should().BeTrue();
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.Pending);
        transaction.IsProcessingSuccess.Should().BeNull();
    }

    /// <summary>
    /// A notification for an object the provider does not know is refused and moves no money.
    /// </summary>
    [Fact]
    public async Task ForgedNotification_ProviderDoesNotKnowThePayment_ReturnsBadRequestAndKeepsBalances()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        AddTransaction(player, TransactionTypes.Deposit, 500);
        _providerMock
            .Setup(x => x.GetPaymentAsync("forged", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProviderOperation?)null);

        var result = await _service.HandleNotificationAsync(Notification("payment.succeeded", "forged"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        PlayerBalance(player).Should().Be(1000);
    }

    /// <summary>
    /// A payment whose amount differs from the transaction amount is refused and the transaction stays unprocessed.
    /// </summary>
    [Fact]
    public async Task PaymentAmountDiffersFromTransaction_ReturnsBadRequestAndKeepsBalances()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var transaction = AddTransaction(player, TransactionTypes.Deposit, 500);
        ProviderReportsPayment(transaction, ProviderOperationStatus.Succeeded, amount: 5000);

        var result = await _service.HandleNotificationAsync(Notification("payment.succeeded", PaymentId));

        result.IsSuccess.Should().BeFalse();
        PlayerBalance(player).Should().Be(1000);
        transaction.IsProcessingSuccess.Should().BeNull();
    }

    /// <summary>
    /// A payment event that points to a withdrawal transaction is refused.
    /// </summary>
    [Fact]
    public async Task PaymentEventForWithdrawalTransaction_ReturnsBadRequest()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var withdrawal = AddTransaction(player, TransactionTypes.Withdrawal, 300);
        ProviderReportsPayment(withdrawal, ProviderOperationStatus.Succeeded);

        var result = await _service.HandleNotificationAsync(Notification("payment.succeeded", PaymentId));

        result.IsSuccess.Should().BeFalse();
        PlayerBalance(player).Should().Be(1000);
    }

    /// <summary>
    /// A payment without the application transaction id in its metadata is refused.
    /// </summary>
    [Fact]
    public async Task PaymentWithoutTransactionId_ReturnsBadRequest()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var transaction = AddTransaction(player, TransactionTypes.Deposit, 500);
        _providerMock
            .Setup(x => x.GetPaymentAsync(PaymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderOperation(PaymentId, ProviderOperationStatus.Succeeded, 500, "rub", null, null));

        var result = await _service.HandleNotificationAsync(Notification("payment.succeeded", PaymentId));

        result.IsSuccess.Should().BeFalse();
        transaction.IsProcessingSuccess.Should().BeNull();
    }

    /// <summary>
    /// A successful deposit pays the inviter a bonus of ten percent of the amount.
    /// </summary>
    [Fact]
    public async Task PaymentSucceeded_PaysTenPercentBonusToInviter()
    {
        var inviter = TestDbContextFactory.CreateTestPlayer(_context, telegramId: 1);
        var invited = TestDbContextFactory.CreateTestPlayer(_context, telegramId: 2);
        AddInvitation(invited, inviter);
        var transaction = AddTransaction(invited, TransactionTypes.Deposit, 500);
        ProviderReportsPayment(transaction, ProviderOperationStatus.Succeeded);

        await _service.HandleNotificationAsync(Notification("payment.succeeded", PaymentId));

        PlayerBalance(invited).Should().Be(1500);
        PlayerBalance(inviter).Should().Be(1050);
    }

    /// <summary>
    /// A banned inviter does not receive the referral bonus.
    /// </summary>
    [Fact]
    public async Task PaymentSucceeded_BannedInviterGetsNoBonus()
    {
        var inviter = TestDbContextFactory.CreateTestPlayer(_context, telegramId: 1);
        var invited = TestDbContextFactory.CreateTestPlayer(_context, telegramId: 2);
        AddInvitation(invited, inviter);
        TestDbContextFactory.AddActiveBan(_context, inviter.Id);
        var transaction = AddTransaction(invited, TransactionTypes.Deposit, 500);
        ProviderReportsPayment(transaction, ProviderOperationStatus.Succeeded);

        await _service.HandleNotificationAsync(Notification("payment.succeeded", PaymentId));

        PlayerBalance(inviter).Should().Be(1000);
    }

    #endregion Payment Tests

    #region Payout Tests

    /// <summary>
    /// A successful payout reduces the application balance and approves the withdrawal.
    /// </summary>
    [Fact]
    public async Task PayoutSucceeded_ReducesAppBalance()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var transaction = AddTransaction(player, TransactionTypes.Withdrawal, 300);
        ProviderReportsPayout(transaction, ProviderOperationStatus.Succeeded);

        var result = await _service.HandleNotificationAsync(Notification("payout.succeeded", PayoutId));

        result.IsSuccess.Should().BeTrue();
        AppBalance().Should().Be(99700);
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.Approved);
    }

    /// <summary>
    /// A canceled payout refunds the player once, even when the notification is delivered twice.
    /// </summary>
    [Fact]
    public async Task PayoutCanceled_DeliveredTwice_RefundsPlayerOnce()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context);
        var transaction = AddTransaction(player, TransactionTypes.Withdrawal, 300);
        ProviderReportsPayout(transaction, ProviderOperationStatus.Canceled, "payout_network/recipient_not_found");
        var request = Notification("payout.canceled", PayoutId);

        await _service.HandleNotificationAsync(request);
        await _service.HandleNotificationAsync(request);

        PlayerBalance(player).Should().Be(1300);
        transaction.TransactionStatusId.Should().Be(TransactionStatuses.Rejected);
        transaction.ProcessingError.Should().Be("payout_network/recipient_not_found");
    }

    #endregion Payout Tests

    #region Other Notifications

    /// <summary>
    /// An event type the service does not handle is acknowledged without calling the provider.
    /// </summary>
    [Fact]
    public async Task UnrelatedEvent_IsAcknowledgedWithoutCallingProvider()
    {
        var result = await _service.HandleNotificationAsync(Notification("refund.succeeded", "refund-1"));

        result.IsSuccess.Should().BeTrue();
        _providerMock.Verify(x => x.GetPaymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _context.ProcessingWebhookLogs.Single().ProcessingStatus.Should().Be("Ignored");
    }

    /// <summary>
    /// A body that is not valid JSON is refused with a bad request.
    /// </summary>
    [Fact]
    public async Task InvalidJson_ReturnsBadRequest()
    {
        var result = await _service.HandleNotificationAsync(new("not json", "POST", "application/json", "127.0.0.1", "tests"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// When the provider cannot be reached the service answers with an internal error, so that the provider retries.
    /// </summary>
    [Fact]
    public async Task ProviderUnavailable_ReturnsInternalError()
    {
        _providerMock
            .Setup(x => x.GetPaymentAsync(PaymentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("down"));

        var result = await _service.HandleNotificationAsync(Notification("payment.succeeded", PaymentId));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(500);
    }

    #endregion Other Notifications
}
