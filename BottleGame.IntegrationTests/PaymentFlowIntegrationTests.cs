using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.Domain.Entities;
using BottleGame.Domain.Enums;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.External.Processing;
using BottleGame.Infrastructure.Services;
using BottleGame.Infrastructure.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BottleGame.IntegrationTests;

/// <summary>
/// Integration tests of the payment flow on a real PostgreSQL database
/// </summary>
public class PaymentFlowIntegrationTests : IClassFixture<PostgresFixture>
{
    /// <summary>
    /// Payment id
    /// </summary>
    private const string PAYMENT_ID = "pay-1";

    /// <summary>
    /// Database
    /// </summary>
    private readonly PostgresFixture _database;

    /// <summary>
    /// Initializes a new instance of <see cref="PaymentFlowIntegrationTests"/>
    /// </summary>
    /// <param name="database">Database</param>
    public PaymentFlowIntegrationTests(PostgresFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Builds a successful payment notification
    /// </summary>
    /// <returns>The created value</returns>
    private static WebhookRequestInfo PaymentSucceededNotification() =>
        new("""{"type":"notification","event":"payment.succeeded","object":{"id":"pay-1"}}""",
            "POST", "application/json", "127.0.0.1", "integration-tests");

    /// <summary>
    /// Creates the notification service on a new database context
    /// </summary>
    /// <param name="context">Context</param>
    /// <param name="provider">Provider</param>
    /// <returns>The created value</returns>
    private PaymentWebhookService CreateService(ApplicationDbContext context, IPaymentProvider provider) =>
        new(context, provider, NullLogger<PaymentWebhookService>.Instance);

    /// <summary>
    /// Every mapped entity can be queried, so schema.sql has all the tables and columns of the EF model
    /// </summary>
    [PostgresFact]
    public async Task SchemaScript_CoversEveryMappedEntity()
    {
        await using var context = _database.CreateContext();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var set = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(context, null)!;

            // The query selects every mapped column, so a missing or extra column in schema.sql makes it fail.
            var rows = ((System.Collections.IEnumerable)set).Cast<object>().ToList();
            Assert.NotNull(rows);
        }
    }

    /// <summary>
    /// seed.sql loads the statuses, types, application balance and game parameters
    /// </summary>
    [PostgresFact]
    public async Task SeedScript_LoadsReferenceData()
    {
        await using var context = _database.CreateContext();

        Assert.Equal([1, 2, 3], await context.TransactionStatuses.OrderBy(s => s.Id).Select(s => s.Id).ToListAsync());
        Assert.Equal([1, 2], await context.TransactionTypes.OrderBy(t => t.Id).Select(t => t.Id).ToListAsync());
        Assert.Equal(1, await context.AppBalance.CountAsync());
        Assert.Equal(1, await context.GameParams.CountAsync());
    }

    /// <summary>
    /// The same successful notification delivered twice credits the balance once
    /// </summary>
    [PostgresFact]
    public async Task PaymentSucceeded_CreditsBalanceOnce_EvenWhenDeliveredTwice()
    {
        var playerId = await _database.AddPlayerAsync(balance: 1000);
        var transactionId = await _database.AddPendingTransactionAsync(playerId, TransactionTypes.DEPOSIT, 500);
        var provider = new StubPaymentProvider(PAYMENT_ID, transactionId, 500);

        for (var delivery = 0; delivery < 2; delivery++)
        {
            await using var context = _database.CreateContext();
            var result = await CreateService(context, provider).HandleNotificationAsync(PaymentSucceededNotification());
            Assert.True(result.IsSuccess);
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(1500, (await verify.PlayersBalances.SingleAsync(b => b.PlayerId == playerId)).Amount);
        Assert.Equal(TransactionStatuses.APPROVED, (await verify.MoneyTransactions.SingleAsync(t => t.Id == transactionId)).TransactionStatusId);
    }

    /// <summary>
    /// Two simultaneous deliveries of the same notification apply it once, thanks to the row version
    /// </summary>
    [PostgresFact]
    public async Task PaymentSucceeded_DeliveredConcurrently_CreditsBalanceOnce()
    {
        for (var attempt = 0; attempt < 15; attempt++)
        {
            var playerId = await _database.AddPlayerAsync(balance: 1000);
            var transactionId = await _database.AddPendingTransactionAsync(playerId, TransactionTypes.DEPOSIT, 500);

            // Both handlers get the provider's answer at the same moment and then read the transaction at the same moment.
            var provider = new StubPaymentProvider(PAYMENT_ID, transactionId, 500, rendezvousParticipants: 2);

            async Task DeliverAsync()
            {
                await using var context = _database.CreateContext();
                var result = await CreateService(context, provider).HandleNotificationAsync(PaymentSucceededNotification());
                Assert.True(result.IsSuccess);
            }

            await Task.WhenAll(Task.Run(DeliverAsync), Task.Run(DeliverAsync));

            await using var verify = _database.CreateContext();
            Assert.Equal(1500, (await verify.PlayersBalances.SingleAsync(b => b.PlayerId == playerId)).Amount);

            // The balance alone could stay correct after a double apply because of a lost update, so the log is checked as well.
            var applied = await verify.ProcessingWebhookLogs
                .CountAsync(l => l.TransactionId == transactionId.ToString() && l.ProcessingStatus == "Success");

            Assert.Equal(1, applied);
        }
    }

    /// <summary>
    /// The referral bonus is paid to the inviter with real foreign keys in place
    /// </summary>
    [PostgresFact]
    public async Task PaymentSucceeded_PaysReferralBonusToInviter_RespectingForeignKeys()
    {
        var inviterId = await _database.AddPlayerAsync(balance: 1000);
        var invitedId = await _database.AddPlayerAsync(balance: 1000);

        await using (var context = _database.CreateContext())
        {
            context.InvitedPlayers.Add(new InvitedPlayer
            {
                PlayerIdInvited = invitedId,
                PlayerIdInvitedBy = inviterId,
                InvitedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }

        var transactionId = await _database.AddPendingTransactionAsync(invitedId, TransactionTypes.DEPOSIT, 500);
        var provider = new StubPaymentProvider(PAYMENT_ID, transactionId, 500);

        await using (var context = _database.CreateContext())
        {
            var result = await CreateService(context, provider).HandleNotificationAsync(PaymentSucceededNotification());
            Assert.True(result.IsSuccess);
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(1500, (await verify.PlayersBalances.SingleAsync(b => b.PlayerId == invitedId)).Amount);
        Assert.Equal(1050, (await verify.PlayersBalances.SingleAsync(b => b.PlayerId == inviterId)).Amount);
    }

    /// <summary>
    /// Two simultaneous approvals of one withdrawal succeed only once
    /// </summary>
    [PostgresFact]
    public async Task ConcurrentWithdrawalApproval_AppliesOnlyOnce()
    {
        var playerId = await _database.AddPlayerAsync(balance: 1000);
        var transactionId = await _database.AddPendingTransactionAsync(playerId, TransactionTypes.WITHDRAWAL, 300);

        async Task<bool> ApproveAsync()
        {
            await using var context = _database.CreateContext();
            var service = new TransactionService(
                context,
                NullLogger<TransactionService>.Instance,
                new FixedCache(await context.GameParams.AsNoTracking().FirstAsync()),
                new BottleGame.Infrastructure.Services.Payments.SimulatedTransactionProcessor(
                    context, NullLogger<BottleGame.Infrastructure.Services.Payments.SimulatedTransactionProcessor>.Instance));

            var result = await service.UpdateTransactionStatus(transactionId, new ChangeTransactionStatusRequest { TransactionStatusId = TransactionStatuses.APPROVED });
            return result.IsSuccess;
        }

        var outcomes = await Task.WhenAll(Task.Run(ApproveAsync), Task.Run(ApproveAsync));

        Assert.Single(outcomes, succeeded => succeeded);

        await using var verify = _database.CreateContext();
        Assert.Equal(TransactionStatuses.APPROVED, (await verify.MoneyTransactions.SingleAsync(t => t.Id == transactionId)).TransactionStatusId);
    }

    /// <summary>
    /// Several simultaneous withdrawals of the whole balance create only one withdrawal, so the balance is never spent twice
    /// </summary>
    [PostgresFact]
    public async Task ConcurrentWithdrawals_CannotSpendTheSameBalanceTwice()
    {
        var playerId = await _database.AddPlayerAsync(balance: 1000);

        async Task<bool> WithdrawAsync()
        {
            await using var context = _database.CreateContext();
            var service = new TransactionService(
                context,
                NullLogger<TransactionService>.Instance,
                new FixedCache(await context.GameParams.AsNoTracking().FirstAsync()),
                new BottleGame.Infrastructure.Services.Payments.SimulatedTransactionProcessor(
                    context, NullLogger<BottleGame.Infrastructure.Services.Payments.SimulatedTransactionProcessor>.Instance));

            var result = await service.CreateTransaction(
                playerId, new MoneyTransactionRequest { Amount = 1000, IsDeposit = false }, playerId, isAdmin: false);
            return result.IsSuccess;
        }

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(WithdrawAsync)));

        Assert.Single(outcomes, succeeded => succeeded);

        await using var verify = _database.CreateContext();
        Assert.Equal(0, (await verify.PlayersBalances.SingleAsync(b => b.PlayerId == playerId)).Amount);
        Assert.Equal(1, await verify.MoneyTransactions.CountAsync(t => t.PlayerId == playerId && t.TransactionTypeId == TransactionTypes.WITHDRAWAL));
    }

    /// <summary>
    /// Banning writes an active ban and a history record, and a second ban of the same player is refused.
    /// The in-memory unit test cannot cover this because the ban history has a row version
    /// </summary>
    [PostgresFact]
    public async Task BanPlayer_CreatesBanAndHistory_AndRefusesSecondBan()
    {
        var playerId = await _database.AddPlayerAsync(balance: 500);

        async Task<int> BanAsync()
        {
            await using var context = _database.CreateContext();
            var service = new BanService(context, NullLogger<BanService>.Instance);
            return (await service.BanPlayer(new BanPlayerRequest { PlayerId = playerId, BanReason = "integration test" })).StatusCode;
        }

        Assert.Equal(200, await BanAsync());
        Assert.Equal(409, await BanAsync());

        await using var verify = _database.CreateContext();
        Assert.True((await verify.Bans.SingleAsync(b => b.PlayerId == playerId)).IsActive);
        Assert.Equal(1, await verify.BansHistory.CountAsync(h => h.PlayerId == playerId));
    }

    /// <summary>
    /// A date is stored as UTC and read back as the same moment with the UTC kind, so the JSON carries a "Z"
    /// and clients in any time zone show the right time
    /// </summary>
    [PostgresFact]
    public async Task Dates_AreStoredAndReadAsUtc()
    {
        var playerId = await _database.AddPlayerAsync(balance: 100);
        var before = DateTime.UtcNow;
        var transactionId = await _database.AddPendingTransactionAsync(playerId, TransactionTypes.DEPOSIT, 100);
        var after = DateTime.UtcNow;

        await using var verify = _database.CreateContext();
        var createdAt = (await verify.MoneyTransactions.AsNoTracking().SingleAsync(t => t.Id == transactionId)).CreatedAt;

        Assert.Equal(DateTimeKind.Utc, createdAt.Kind);
        Assert.InRange(createdAt, before.AddSeconds(-2), after.AddSeconds(2));

        var storedUtc = await verify.Database
            .SqlQuery<DateTime>($"""SELECT ("CreatedAt" AT TIME ZONE 'UTC') AS "Value" FROM "MoneyTransactions" WHERE "Id" = {transactionId}""")
            .SingleAsync();
        Assert.InRange(storedUtc, before.AddSeconds(-2), after.AddSeconds(2));
    }

    /// <summary>
    /// A reject that arrives while the payout request is in flight sees the saved payout state and is refused, so the player is not refunded for a payout that may be real
    /// </summary>
    [PostgresFact]
    public async Task RejectDuringPayoutRequest_IsRefusedWithoutRefund()
    {
        var playerId = await _database.AddPlayerAsync(balance: 1000);
        var transactionId = await _database.AddPendingTransactionAsync(playerId, TransactionTypes.WITHDRAWAL, 300);
        BottleGame.Application.Common.Result? rejectResult = null;

        async Task<TransactionService> CreateLiveServiceAsync(ApplicationDbContext context, IPaymentProvider provider) =>
            new(context,
                NullLogger<TransactionService>.Instance,
                new FixedCache(await context.GameParams.AsNoTracking().FirstAsync()),
                new LiveTransactionProcessor(context, NullLogger<LiveTransactionProcessor>.Instance, provider, Options.Create(new ProcessingOptions())));

        var provider = new TimeoutPayoutProvider(async () =>
        {
            await using var rejectContext = _database.CreateContext();
            var rejectService = await CreateLiveServiceAsync(rejectContext, new TimeoutPayoutProvider(() => Task.CompletedTask));
            rejectResult = await rejectService.UpdateTransactionStatus(transactionId, new ChangeTransactionStatusRequest { TransactionStatusId = TransactionStatuses.REJECTED });
        });

        await using (var context = _database.CreateContext())
        {
            var service = await CreateLiveServiceAsync(context, provider);
            await service.UpdateTransactionStatus(transactionId, new ChangeTransactionStatusRequest { TransactionStatusId = TransactionStatuses.APPROVED });
        }

        Assert.NotNull(rejectResult);
        Assert.Equal(409, rejectResult.StatusCode);

        await using var verify = _database.CreateContext();
        var transaction = await verify.MoneyTransactions.AsNoTracking().SingleAsync(t => t.Id == transactionId);
        Assert.Equal(TransactionStatuses.PENDING, transaction.TransactionStatusId);
        Assert.Equal(PayoutState.OutcomeUnknown, transaction.PayoutState);
        Assert.NotNull(transaction.PayoutRequestedAt);
        Assert.Equal("sbp", transaction.PaymentGateway);
        Assert.Equal("79001234567:100000000111", transaction.PayoutWallet);
        Assert.Null(transaction.ProcessingError);
        Assert.Equal(1000, (await verify.PlayersBalances.AsNoTracking().SingleAsync(b => b.PlayerId == playerId)).Amount);
    }
}
