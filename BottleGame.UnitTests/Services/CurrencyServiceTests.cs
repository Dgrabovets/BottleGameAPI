using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.External.Processing;
using BottleGame.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Тесты для CurrencyService в режимах Simulated (без процессинга) и Live.
/// </summary>
public class CurrencyServiceTests : IDisposable
{
    /// <summary>
    /// Mock of the payment provider.
    /// </summary>
    private readonly Mock<IPaymentProvider> _paymentProviderMock;

    /// <summary>
    /// Memory cache used by the service.
    /// </summary>
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Mock of the logger.
    /// </summary>
    private readonly Mock<ILogger<CurrencyService>> _loggerMock;

    /// <summary>
    /// The service under test.
    /// </summary>
    private readonly CurrencyService _service;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test.
    /// </summary>
    public CurrencyServiceTests()
    {
        _paymentProviderMock = new Mock<IPaymentProvider>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<CurrencyService>>();

        _service = CreateService(ProcessingMode.Simulated);
    }

    /// <summary>
    /// Creates the notification service on a new database context.
    /// </summary>
    /// <param name="mode">Mode.</param>
    /// <returns>The created value.</returns>
    private CurrencyService CreateService(ProcessingMode mode) =>
        new(_paymentProviderMock.Object, _cache, _loggerMock.Object, Options.Create(new ProcessingOptions { Mode = mode }));

    /// <summary>
    /// Releases the database context used by the tests.
    /// </summary>
    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAvailableCurrenciesAsync Tests

    /// <summary>
    /// Get available currencies async: returns test currency.
    /// </summary>
    [Fact]
    public async Task GetAvailableCurrenciesAsync_ReturnsTestCurrency()
    {
        var result = await _service.GetAvailableCurrenciesAsync();

        result.Should().HaveCount(1);
        result.Should().ContainSingle(c => c.Code == "rub" && c.IsActive);
        _paymentProviderMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Get available currencies async: returns same data on repeated calls.
    /// </summary>
    [Fact]
    public async Task GetAvailableCurrenciesAsync_ReturnsSameDataOnRepeatedCalls()
    {
        var first = await _service.GetAvailableCurrenciesAsync();
        var second = await _service.GetAvailableCurrenciesAsync();

        first.Should().BeEquivalentTo(second);
        _paymentProviderMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetPaymentGatewaysAsync Tests

    /// <summary>
    /// Get payment gateways async: returns test gateway.
    /// </summary>
    [Fact]
    public async Task GetPaymentGatewaysAsync_ReturnsTestGateway()
    {
        var result = await _service.GetPaymentGatewaysAsync();

        result.Should().HaveCount(1);
        result.First().Code.Should().Be("sberbank");
        result.First().Currency.Should().Be("rub");
        _paymentProviderMock.Verify(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetPaymentGatewaysForCurrencyAsync Tests

    /// <summary>
    /// Get payment gateways for currency async: filters by currency.
    /// </summary>
    [Fact]
    public async Task GetPaymentGatewaysForCurrencyAsync_FiltersByCurrency()
    {
        var rubResult = await _service.GetPaymentGatewaysForCurrencyAsync("rub");
        var kztResult = await _service.GetPaymentGatewaysForCurrencyAsync("kzt");

        rubResult.Should().HaveCount(1);
        rubResult.First().Code.Should().Be("sberbank");
        kztResult.Should().BeEmpty();
    }

    /// <summary>
    /// Get payment gateways for currency async: excludes non payment gateways.
    /// </summary>
    [Fact]
    public async Task GetPaymentGatewaysForCurrencyAsync_ExcludesNonPaymentGateways()
    {
        var result = await _service.GetPaymentGatewaysForCurrencyAsync("rub");

        result.Should().HaveCount(1);
        result.First().SupportsPayments.Should().BeTrue();
    }

    #endregion

    #region GetPayoutGatewaysForCurrencyAsync Tests

    /// <summary>
    /// Get payout gateways for currency async: filters correctly.
    /// </summary>
    [Fact]
    public async Task GetPayoutGatewaysForCurrencyAsync_FiltersCorrectly()
    {
        var result = await _service.GetPayoutGatewaysForCurrencyAsync("rub");

        result.Should().HaveCount(1);
        result.First().Code.Should().Be("sberbank");
        result.First().SupportsPayouts.Should().BeTrue();
    }

    #endregion

    #region GetExchangeRateAsync Tests

    /// <summary>
    /// Get exchange rate async: returns one in simulated mode.
    /// </summary>
    [Fact]
    public async Task GetExchangeRateAsync_ReturnsOneInSimulatedMode()
    {
        var result = await _service.GetExchangeRateAsync("rub");

        result.Should().Be(1m);
        _paymentProviderMock.Verify(
            x => x.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region ConvertToBaseCurrencyAsync Tests

    /// <summary>
    /// Convert to base currency async: with unit rate; returns same amount.
    /// </summary>
    [Fact]
    public async Task ConvertToBaseCurrencyAsync_WithUnitRate_ReturnsSameAmount()
    {
        var result = await _service.ConvertToBaseCurrencyAsync(900m, "rub");

        result.Should().Be(900m);
    }

    #endregion

    #region ConvertFromBaseCurrencyAsync Tests

    /// <summary>
    /// Convert from base currency async: with unit rate; returns same amount.
    /// </summary>
    [Fact]
    public async Task ConvertFromBaseCurrencyAsync_WithUnitRate_ReturnsSameAmount()
    {
        var result = await _service.ConvertFromBaseCurrencyAsync(10m, "rub");

        result.Should().Be(10m);
    }

    #endregion

    #region IsCurrencyActiveAsync Tests

    /// <summary>
    /// Is currency active async: returns true for test currency.
    /// </summary>
    [Fact]
    public async Task IsCurrencyActiveAsync_ReturnsTrueForTestCurrency()
    {
        var result = await _service.IsCurrencyActiveAsync("rub");

        result.Should().BeTrue();
    }

    /// <summary>
    /// Is currency active async: returns false for unknown currency.
    /// </summary>
    [Fact]
    public async Task IsCurrencyActiveAsync_ReturnsFalseForUnknownCurrency()
    {
        var result = await _service.IsCurrencyActiveAsync("usd");

        result.Should().BeFalse();
    }

    #endregion

    #region IsAmountValidForGatewayAsync Tests

    /// <summary>
    /// Is amount valid for gateway async: returns true for valid amount.
    /// </summary>
    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsTrueForValidAmount()
    {
        var result = await _service.IsAmountValidForGatewayAsync("sberbank", 5000);

        result.Should().BeTrue();
    }

    /// <summary>
    /// Is amount valid for gateway async: returns false for amount below min.
    /// </summary>
    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsFalseForAmountBelowMin()
    {
        var result = await _service.IsAmountValidForGatewayAsync("sberbank", 50);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Is amount valid for gateway async: returns false for amount above max.
    /// </summary>
    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsFalseForAmountAboveMax()
    {
        var result = await _service.IsAmountValidForGatewayAsync("sberbank", 2_000_000);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Is amount valid for gateway async: returns false for unknown gateway.
    /// </summary>
    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsFalseForUnknownGateway()
    {
        var result = await _service.IsAmountValidForGatewayAsync("unknown", 5000);

        result.Should().BeFalse();
    }

    #endregion

    #region Live Mode Tests

    /// <summary>
    /// Live: get available currencies async; loads from processing once and caches.
    /// </summary>
    [Fact]
    public async Task Live_GetAvailableCurrenciesAsync_LoadsFromProcessingOnceAndCaches()
    {
        _paymentProviderMock
            .Setup(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CurrencyInfo("usd", "US dollar", isActive: true)]);
        var service = CreateService(ProcessingMode.Live);

        var first = await service.GetAvailableCurrenciesAsync();
        var second = await service.GetAvailableCurrenciesAsync();

        first.Should().ContainSingle(c => c.Code == "usd");
        second.Should().BeEquivalentTo(first);
        _paymentProviderMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Live: get available currencies async; force refresh reloads from processing.
    /// </summary>
    [Fact]
    public async Task Live_GetAvailableCurrenciesAsync_ForceRefreshReloadsFromProcessing()
    {
        _paymentProviderMock
            .Setup(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CurrencyInfo("usd", "US dollar", isActive: true)]);
        var service = CreateService(ProcessingMode.Live);

        await service.GetAvailableCurrenciesAsync();
        await service.GetAvailableCurrenciesAsync(forceRefresh: true);

        _paymentProviderMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>
    /// Live: convert to base currency async; uses rate from processing.
    /// </summary>
    [Fact]
    public async Task Live_ConvertToBaseCurrencyAsync_UsesRateFromProcessing()
    {
        _paymentProviderMock
            .Setup(x => x.GetExchangeRateAsync("usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(90m);
        var service = CreateService(ProcessingMode.Live);

        var result = await service.ConvertToBaseCurrencyAsync(900m, "usd");

        result.Should().Be(10m);
    }

    #endregion

    #region ClearCache Tests

    /// <summary>
    /// Clear cache: does not call processing api.
    /// </summary>
    [Fact]
    public async Task ClearCache_DoesNotCallProcessingApi()
    {
        await _service.GetAvailableCurrenciesAsync();
        await _service.GetPaymentGatewaysAsync();

        _service.ClearCache();

        await _service.GetAvailableCurrenciesAsync();
        await _service.GetPaymentGatewaysAsync();

        _paymentProviderMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _paymentProviderMock.Verify(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
