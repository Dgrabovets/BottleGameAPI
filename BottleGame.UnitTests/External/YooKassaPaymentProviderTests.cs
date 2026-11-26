using BottleGame.Application.DTOs.Processing;
using BottleGame.Infrastructure.External.Processing;
using BottleGame.Infrastructure.External.YooKassa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BottleGame.UnitTests.External;

/// <summary>
/// Tests for the YooKassa payment provider against a fake HTTP handler
/// </summary>
public class YooKassaPaymentProviderTests
{
    /// <summary>
    /// Recording HTTP handler that stands in for the YooKassa API
    /// </summary>
    private readonly RecordingHttpHandler _handler = new();

    /// <summary>
    /// Creates the provider on top of the recording handler, with optional changes to the options
    /// </summary>
    /// <param name="configure">Configure</param>
    /// <returns>The created value</returns>
    private YooKassaPaymentProvider CreateProvider(Action<ProcessingOptions>? configure = null)
    {
        var options = new ProcessingOptions
        {
            Mode = ProcessingMode.Live,
            ShopId = "shop-1",
            SecretKey = "secret-1",
            PayoutAgentId = "agent-1",
            PayoutSecretKey = "agent-secret-1",
            ReturnUrl = "https://example.com/return"
        };

        configure?.Invoke(options);

        var client = new HttpClient(_handler) { BaseAddress = new Uri(options.BaseUrl) };
        return new YooKassaPaymentProvider(client, Options.Create(options), NullLogger<YooKassaPaymentProvider>.Instance);
    }

    /// <summary>
    /// Builds the expected Basic authorization header value
    /// </summary>
    /// <param name="login">Login</param>
    /// <param name="secret">Secret</param>
    /// <returns>The created value</returns>
    private static string Basic(string login, string secret) =>
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}:{secret}"));

    #region CreatePayment Tests

    /// <summary>
    /// Creating a payment sends the shop credentials, an idempotence key built from the transaction id, the amount and the transaction id in the metadata
    /// </summary>
    [Fact]
    public async Task CreatePayment_SendsShopCredentialsIdempotenceKeyAndMetadata()
    {
        _handler.Respond(HttpStatusCode.OK, """
            {"id":"pay-1","status":"pending","confirmation":{"type":"redirect","confirmation_url":"https://yoomoney.ru/pay/1"}}
            """);

        var result = await CreateProvider().CreatePaymentAsync(new CreatePaymentRequest(42, 500m, "rub", "Deposit"));

        result.Should().Be(new CreatePaymentResult(true, "pay-1", "https://yoomoney.ru/pay/1", null));
        var sent = _handler.Single();
        sent.Method.Should().Be(HttpMethod.Post);
        sent.Uri.Should().Be("https://api.yookassa.ru/v3/payments");
        sent.Authorization.Should().Be(Basic("shop-1", "secret-1"));
        sent.IdempotenceKey.Should().Be("payment-42");

        using var body = JsonDocument.Parse(sent.Body!);
        body.RootElement.GetProperty("amount").GetProperty("value").GetString().Should().Be("500.00");
        body.RootElement.GetProperty("amount").GetProperty("currency").GetString().Should().Be("RUB");
        body.RootElement.GetProperty("capture").GetBoolean().Should().BeTrue();
        body.RootElement.GetProperty("confirmation").GetProperty("type").GetString().Should().Be("redirect");
        body.RootElement.GetProperty("confirmation").GetProperty("return_url").GetString().Should().Be("https://example.com/return");
        body.RootElement.GetProperty("metadata").GetProperty("transaction_id").GetString().Should().Be("42");
    }

    /// <summary>
    /// An API error response becomes a failed result that carries the provider's description
    /// </summary>
    [Fact]
    public async Task CreatePayment_ApiError_ReturnsFailureWithDescription()
    {
        _handler.Respond(HttpStatusCode.BadRequest, """{"type":"error","code":"invalid_request","description":"Amount is too small"}""");

        var result = await CreateProvider().CreatePaymentAsync(new CreatePaymentRequest(42, 0.5m, "rub", "x"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Amount is too small");
    }

    /// <summary>
    /// A description longer than 128 characters is cut to the length YooKassa accepts
    /// </summary>
    [Fact]
    public async Task CreatePayment_TruncatesLongDescriptionTo128Characters()
    {
        _handler.Respond(HttpStatusCode.OK, """{"id":"pay-1","status":"pending","confirmation":{"confirmation_url":"https://pay"}}""");

        await CreateProvider().CreatePaymentAsync(new CreatePaymentRequest(1, 100m, "rub", new string('a', 300)));

        using var body = JsonDocument.Parse(_handler.Single().Body!);
        body.RootElement.GetProperty("description").GetString()!.Length.Should().Be(128);
    }

    #endregion CreatePayment Tests

    #region CreatePayout Tests

    /// <summary>
    /// An SBP payout uses the payout gateway credentials and sends the normalized phone number and the bank id
    /// </summary>
    [Fact]
    public async Task CreatePayout_Sbp_SendsGatewayCredentialsAndDestination()
    {
        _handler.Respond(HttpStatusCode.OK, """{"id":"payout-1","status":"pending"}""");

        var result = await CreateProvider().CreatePayoutAsync(
            new CreatePayoutRequest(7, 300m, "rub", "sbp", "+7 (900) 123-45-67:100000000111", "Withdrawal"));

        result.Should().Be(new CreatePayoutResult(true, "payout-1", null));
        var sent = _handler.Single();
        sent.Uri.Should().Be("https://api.yookassa.ru/v3/payouts");
        sent.Authorization.Should().Be(Basic("agent-1", "agent-secret-1"));
        sent.IdempotenceKey.Should().Be("payout-7");

        using var body = JsonDocument.Parse(sent.Body!);
        var destination = body.RootElement.GetProperty("payout_destination_data");
        destination.GetProperty("type").GetString().Should().Be("sbp");
        destination.GetProperty("phone").GetString().Should().Be("79001234567");
        destination.GetProperty("bank_id").GetString().Should().Be("100000000111");
        body.RootElement.GetProperty("metadata").GetProperty("transaction_id").GetString().Should().Be("7");
    }

    /// <summary>
    /// Without payout gateway credentials the payout fails and the API is not called
    /// </summary>
    [Fact]
    public async Task CreatePayout_PayoutsNotConfigured_FailsWithoutCallingApi()
    {
        var provider = CreateProvider(o => o.PayoutAgentId = string.Empty);

        var result = await provider.CreatePayoutAsync(new CreatePayoutRequest(7, 300m, "rub", "sbp", "79001234567:1", "Withdrawal"));

        result.IsSuccess.Should().BeFalse();
        _handler.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// A malformed payout destination fails before any request is made
    /// </summary>
    /// <param name="destination">Destination</param>
    [Theory]
    [InlineData("79001234567")]
    [InlineData("123:100000000111")]
    [InlineData("79001234567:")]
    public async Task CreatePayout_MalformedDestination_FailsWithoutCallingApi(string destination)
    {
        var result = await CreateProvider().CreatePayoutAsync(new CreatePayoutRequest(7, 300m, "rub", "sbp", destination, "Withdrawal"));

        result.IsSuccess.Should().BeFalse();
        _handler.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// A payout method other than SBP fails before any request is made
    /// </summary>
    [Fact]
    public async Task CreatePayout_UnsupportedGateway_FailsWithoutCallingApi()
    {
        var result = await CreateProvider().CreatePayoutAsync(new CreatePayoutRequest(7, 300m, "rub", "card", "79001234567:1", "Withdrawal"));

        result.IsSuccess.Should().BeFalse();
        _handler.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// A 4xx answer is a definite rejection and comes back as a failed result
    /// </summary>
    [Fact]
    public async Task CreatePayout_ClientError_ReturnsFailure()
    {
        _handler.Respond(HttpStatusCode.BadRequest, """{"type":"error","code":"invalid_request","description":"Bad bank id"}""");

        var result = await CreateProvider().CreatePayoutAsync(new CreatePayoutRequest(7, 300m, "rub", "sbp", "79001234567:1", "Withdrawal"));

        result.Should().Be(new CreatePayoutResult(false, null, "Bad bank id"));
    }

    /// <summary>
    /// A 5xx answer or a 2xx answer without an id does not say whether the payout exists, so it throws instead of failing
    /// </summary>
    /// <param name="status">Status code of the answer</param>
    /// <param name="body">Body of the answer</param>
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, """{"type":"error","code":"internal_server_error"}""")]
    [InlineData(HttpStatusCode.Accepted, """{"type":"processing","retry_after":1800}""")]
    public async Task CreatePayout_OutcomeUnknown_Throws(HttpStatusCode status, string body)
    {
        _handler.Respond(status, body);

        var act = () => CreateProvider().CreatePayoutAsync(new CreatePayoutRequest(7, 300m, "rub", "sbp", "79001234567:1", "Withdrawal"));

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion CreatePayout Tests

    #region Lookup Tests

    /// <summary>
    /// A payment from the API is mapped to the provider's status, amount, currency and transaction id
    /// </summary>
    /// <param name="status">Status</param>
    /// <param name="expected">Expected</param>
    [Theory]
    [InlineData("succeeded", ProviderOperationStatus.Succeeded)]
    [InlineData("canceled", ProviderOperationStatus.Canceled)]
    [InlineData("pending", ProviderOperationStatus.Pending)]
    [InlineData("waiting_for_capture", ProviderOperationStatus.Pending)]
    public async Task GetPayment_MapsStatusAmountAndTransactionId(string status, ProviderOperationStatus expected)
    {
        _handler.Respond(HttpStatusCode.OK, $$$"""
            {"id":"pay-1","status":"{{{status}}}","amount":{"value":"500.00","currency":"RUB"},"metadata":{"transaction_id":"42"}}
            """);

        var payment = await CreateProvider().GetPaymentAsync("pay-1");

        payment.Should().Be(new ProviderOperation("pay-1", expected, 500m, "rub", 42, null));
        _handler.Single().Uri.Should().Be("https://api.yookassa.ru/v3/payments/pay-1");
        _handler.Single().Authorization.Should().Be(Basic("shop-1", "secret-1"));
    }

    /// <summary>
    /// A canceled payout carries its cancellation reason and is requested with the payout gateway credentials
    /// </summary>
    [Fact]
    public async Task GetPayout_ReadsCancellationReasonAndUsesGatewayCredentials()
    {
        _handler.Respond(HttpStatusCode.OK, """
            {"id":"payout-1","status":"canceled","amount":{"value":"300.00","currency":"RUB"},"metadata":{"transaction_id":"7"},
             "cancellation_details":{"party":"payout_network","reason":"recipient_not_found"}}
            """);

        var payout = await CreateProvider().GetPayoutAsync("payout-1");

        payout!.Status.Should().Be(ProviderOperationStatus.Canceled);
        payout.CancellationReason.Should().Be("payout_network/recipient_not_found");
        payout.TransactionId.Should().Be(7);
        _handler.Single().Authorization.Should().Be(Basic("agent-1", "agent-secret-1"));
    }

    /// <summary>
    /// A payment whose metadata has no transaction id is returned with a null transaction id
    /// </summary>
    [Fact]
    public async Task GetPayment_MetadataWithoutTransactionId_ReturnsNullTransactionId()
    {
        _handler.Respond(HttpStatusCode.OK, """{"id":"pay-1","status":"succeeded","amount":{"value":"1.00","currency":"RUB"}}""");

        var payment = await CreateProvider().GetPaymentAsync("pay-1");

        payment!.TransactionId.Should().BeNull();
    }

    /// <summary>
    /// A payment the API does not know is returned as null
    /// </summary>
    [Fact]
    public async Task GetPayment_NotFound_ReturnsNull()
    {
        _handler.Respond(HttpStatusCode.NotFound, """{"type":"error","code":"not_found","description":"Payment not found"}""");

        var payment = await CreateProvider().GetPaymentAsync("missing");

        payment.Should().BeNull();
    }

    /// <summary>
    /// A server error is raised as an exception, so that the caller can retry
    /// </summary>
    [Fact]
    public async Task GetPayment_ServerError_Throws()
    {
        _handler.Respond(HttpStatusCode.InternalServerError, """{"type":"error","code":"internal_server_error"}""");

        var act = () => CreateProvider().GetPaymentAsync("pay-1");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// An identifier with path characters is refused before any request is sent, because it comes from an unsigned notification
    /// </summary>
    [Theory]
    [InlineData("../payouts/po-1")]
    [InlineData("pay-1/../../x")]
    [InlineData("pay-1?x=1")]
    [InlineData("pay 1")]
    [InlineData("")]
    public async Task GetPayment_MalformedId_ReturnsNullWithoutRequest(string id)
    {
        var payment = await CreateProvider().GetPaymentAsync(id);

        payment.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
    }

    #endregion Lookup Tests

    #region Catalog Tests

    /// <summary>
    /// The catalog offers only rubles, one payment gateway and SBP payouts
    /// </summary>
    [Fact]
    public async Task Catalog_OnlyRubWithPaymentGatewayAndSbpPayouts()
    {
        var provider = CreateProvider();

        (await provider.GetCurrenciesAsync()).Should().ContainSingle(c => c.Code == "rub");
        var gateways = (await provider.GetPaymentGatewaysAsync()).ToList();
        gateways.Should().ContainSingle(g => g.SupportsPayments && !g.SupportsPayouts);
        gateways.Should().ContainSingle(g => g.Code == "sbp" && g.SupportsPayouts && !g.SupportsPayments);
        (await provider.GetExchangeRateAsync("RUB")).Should().Be(1m);
        (await provider.GetExchangeRateAsync("usd")).Should().Be(0m);
    }

    #endregion Catalog Tests
}
