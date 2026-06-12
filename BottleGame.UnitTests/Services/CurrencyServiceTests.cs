using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Тесты для CurrencyService (режим TEST BYPASS без процессинга).
/// </summary>
public class CurrencyServiceTests : IDisposable
{
    private readonly Mock<IProcessingService> _processingServiceMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CurrencyService>> _loggerMock;
    private readonly CurrencyService _service;

    public CurrencyServiceTests()
    {
        _processingServiceMock = new Mock<IProcessingService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<CurrencyService>>();

        _service = new CurrencyService(_processingServiceMock.Object, _cache, _loggerMock.Object);
    }

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAvailableCurrenciesAsync Tests

    [Fact]
    public async Task GetAvailableCurrenciesAsync_ReturnsTestCurrency()
    {
        var result = await _service.GetAvailableCurrenciesAsync();

        result.Should().HaveCount(1);
        result.Should().ContainSingle(c => c.Code == "rub" && c.IsActive);
        _processingServiceMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAvailableCurrenciesAsync_ReturnsSameDataOnRepeatedCalls()
    {
        var first = await _service.GetAvailableCurrenciesAsync();
        var second = await _service.GetAvailableCurrenciesAsync();

        first.Should().BeEquivalentTo(second);
        _processingServiceMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetPaymentGatewaysAsync Tests

    [Fact]
    public async Task GetPaymentGatewaysAsync_ReturnsTestGateway()
    {
        var result = await _service.GetPaymentGatewaysAsync();

        result.Should().HaveCount(1);
        result.First().Code.Should().Be("sberbank");
        result.First().Currency.Should().Be("rub");
        _processingServiceMock.Verify(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetPaymentGatewaysForCurrencyAsync Tests

    [Fact]
    public async Task GetPaymentGatewaysForCurrencyAsync_FiltersByCurrency()
    {
        var rubResult = await _service.GetPaymentGatewaysForCurrencyAsync("rub");
        var kztResult = await _service.GetPaymentGatewaysForCurrencyAsync("kzt");

        rubResult.Should().HaveCount(1);
        rubResult.First().Code.Should().Be("sberbank");
        kztResult.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaymentGatewaysForCurrencyAsync_ExcludesNonPaymentGateways()
    {
        var result = await _service.GetPaymentGatewaysForCurrencyAsync("rub");

        result.Should().HaveCount(1);
        result.First().SupportsPayments.Should().BeTrue();
    }

    #endregion

    #region GetPayoutGatewaysForCurrencyAsync Tests

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

    [Fact]
    public async Task GetExchangeRateAsync_ReturnsOneInTestBypass()
    {
        var result = await _service.GetExchangeRateAsync("rub");

        result.Should().Be(1m);
        _processingServiceMock.Verify(
            x => x.GetMerchantExchangeRateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region ConvertToBaseCurrencyAsync Tests

    [Fact]
    public async Task ConvertToBaseCurrencyAsync_WithUnitRate_ReturnsSameAmount()
    {
        var result = await _service.ConvertToBaseCurrencyAsync(900m, "rub");

        result.Should().Be(900m);
    }

    #endregion

    #region ConvertFromBaseCurrencyAsync Tests

    [Fact]
    public async Task ConvertFromBaseCurrencyAsync_WithUnitRate_ReturnsSameAmount()
    {
        var result = await _service.ConvertFromBaseCurrencyAsync(10m, "rub");

        result.Should().Be(10m);
    }

    #endregion

    #region IsCurrencyActiveAsync Tests

    [Fact]
    public async Task IsCurrencyActiveAsync_ReturnsTrueForTestCurrency()
    {
        var result = await _service.IsCurrencyActiveAsync("rub");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsCurrencyActiveAsync_ReturnsFalseForUnknownCurrency()
    {
        var result = await _service.IsCurrencyActiveAsync("usd");

        result.Should().BeFalse();
    }

    #endregion

    #region IsAmountValidForGatewayAsync Tests

    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsTrueForValidAmount()
    {
        var result = await _service.IsAmountValidForGatewayAsync("sberbank", 5000);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsFalseForAmountBelowMin()
    {
        var result = await _service.IsAmountValidForGatewayAsync("sberbank", 50);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsFalseForAmountAboveMax()
    {
        var result = await _service.IsAmountValidForGatewayAsync("sberbank", 2_000_000);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsFalseForUnknownGateway()
    {
        var result = await _service.IsAmountValidForGatewayAsync("unknown", 5000);

        result.Should().BeFalse();
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public async Task ClearCache_DoesNotCallProcessingApi()
    {
        await _service.GetAvailableCurrenciesAsync();
        await _service.GetPaymentGatewaysAsync();

        _service.ClearCache();

        await _service.GetAvailableCurrenciesAsync();
        await _service.GetPaymentGatewaysAsync();

        _processingServiceMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _processingServiceMock.Verify(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
