using BottleGame.Application.DTOs.Processing;
using BottleGame.Application.Interfaces;
using BottleGame.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Тесты для CurrencyService.
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
    public async Task GetAvailableCurrenciesAsync_ReturnsCurrencies()
    {
        // Arrange
        var currencies = new List<CurrencyInfo>
        {
            new("rub", "Russian Ruble", true),
            new("kzt", "Kazakhstani Tenge", true),
            new("usd", "US Dollar", false)
        };

        _processingServiceMock
            .Setup(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies);

        // Act
        var result = await _service.GetAvailableCurrenciesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(c => c.Code == "rub");
    }

    [Fact]
    public async Task GetAvailableCurrenciesAsync_UsesCaching()
    {
        // Arrange
        var currencies = new List<CurrencyInfo>
        {
            new("rub", "Russian Ruble", true)
        };

        _processingServiceMock
            .Setup(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies);

        // Act
        await _service.GetAvailableCurrenciesAsync();
        await _service.GetAvailableCurrenciesAsync();

        // Assert - should only call API once due to caching
        _processingServiceMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailableCurrenciesAsync_ForceRefresh_BypassesCache()
    {
        // Arrange
        var currencies = new List<CurrencyInfo>
        {
            new("rub", "Russian Ruble", true)
        };

        _processingServiceMock
            .Setup(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies);

        // Act
        await _service.GetAvailableCurrenciesAsync();
        await _service.GetAvailableCurrenciesAsync(forceRefresh: true);

        // Assert - should call API twice
        _processingServiceMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region GetPaymentGatewaysAsync Tests

    [Fact]
    public async Task GetPaymentGatewaysAsync_ReturnsGateways()
    {
        // Arrange
        var gateways = new List<PaymentGatewayInfo>
        {
            new("sberbank", "Sberbank", "rub", true, true, 100, 100000),
            new("alfabank", "Alfa Bank", "rub", true, false, 100, 50000)
        };

        _processingServiceMock
            .Setup(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        // Act
        var result = await _service.GetPaymentGatewaysAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetPaymentGatewaysForCurrencyAsync Tests

    [Fact]
    public async Task GetPaymentGatewaysForCurrencyAsync_FiltersCorrectly()
    {
        // Arrange
        var gateways = new List<PaymentGatewayInfo>
        {
            new("sberbank", "Sberbank", "rub", true, true, 100, 100000),
            new("kaspi", "Kaspi", "kzt", true, true, 1000, 500000)
        };

        _processingServiceMock
            .Setup(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        // Act
        var result = await _service.GetPaymentGatewaysForCurrencyAsync("rub");

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("sberbank");
    }

    [Fact]
    public async Task GetPaymentGatewaysForCurrencyAsync_ExcludesNonPaymentGateways()
    {
        // Arrange
        var gateways = new List<PaymentGatewayInfo>
        {
            new("sberbank", "Sberbank", "rub", true, true, 100, 100000),
            new("payout_only", "Payout Only", "rub", false, true, 100, 50000)
        };

        _processingServiceMock
            .Setup(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        // Act
        var result = await _service.GetPaymentGatewaysForCurrencyAsync("rub");

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("sberbank");
    }

    #endregion

    #region GetPayoutGatewaysForCurrencyAsync Tests

    [Fact]
    public async Task GetPayoutGatewaysForCurrencyAsync_FiltersCorrectly()
    {
        // Arrange
        var gateways = new List<PaymentGatewayInfo>
        {
            new("sberbank", "Sberbank", "rub", true, true, 100, 100000),
            new("payment_only", "Payment Only", "rub", true, false, 100, 50000)
        };

        _processingServiceMock
            .Setup(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        // Act
        var result = await _service.GetPayoutGatewaysForCurrencyAsync("rub");

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("sberbank");
    }

    #endregion

    #region GetExchangeRateAsync Tests

    [Fact]
    public async Task GetExchangeRateAsync_ReturnsRate()
    {
        // Arrange
        _processingServiceMock
            .Setup(x => x.GetMerchantExchangeRateAsync("rub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(90.5m);

        // Act
        var result = await _service.GetExchangeRateAsync("rub");

        // Assert
        result.Should().Be(90.5m);
    }

    [Fact]
    public async Task GetExchangeRateAsync_UsesCaching()
    {
        // Arrange
        _processingServiceMock
            .Setup(x => x.GetMerchantExchangeRateAsync("rub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(90.5m);

        // Act
        await _service.GetExchangeRateAsync("rub");
        await _service.GetExchangeRateAsync("rub");

        // Assert
        _processingServiceMock.Verify(x => x.GetMerchantExchangeRateAsync("rub", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ConvertToBaseCurrencyAsync Tests

    [Fact]
    public async Task ConvertToBaseCurrencyAsync_ConvertsCorrectly()
    {
        // Arrange
        _processingServiceMock
            .Setup(x => x.GetMerchantExchangeRateAsync("rub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(90m);

        // Act
        var result = await _service.ConvertToBaseCurrencyAsync(900m, "rub");

        // Assert
        result.Should().Be(10m);
    }

    [Fact]
    public async Task ConvertToBaseCurrencyAsync_ReturnsOriginalAmountWhenRateIsZero()
    {
        // Arrange
        _processingServiceMock
            .Setup(x => x.GetMerchantExchangeRateAsync("xxx", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // Act
        var result = await _service.ConvertToBaseCurrencyAsync(100m, "xxx");

        // Assert
        result.Should().Be(100m);
    }

    #endregion

    #region ConvertFromBaseCurrencyAsync Tests

    [Fact]
    public async Task ConvertFromBaseCurrencyAsync_ConvertsCorrectly()
    {
        // Arrange
        _processingServiceMock
            .Setup(x => x.GetMerchantExchangeRateAsync("rub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(90m);

        // Act
        var result = await _service.ConvertFromBaseCurrencyAsync(10m, "rub");

        // Assert
        result.Should().Be(900m);
    }

    #endregion

    #region IsCurrencyActiveAsync Tests

    [Fact]
    public async Task IsCurrencyActiveAsync_ReturnsTrueForActiveCurrency()
    {
        // Arrange
        var currencies = new List<CurrencyInfo>
        {
            new("rub", "Russian Ruble", true),
            new("usd", "US Dollar", false)
        };

        _processingServiceMock
            .Setup(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies);

        // Act
        var result = await _service.IsCurrencyActiveAsync("rub");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsCurrencyActiveAsync_ReturnsFalseForInactiveCurrency()
    {
        // Arrange
        var currencies = new List<CurrencyInfo>
        {
            new("rub", "Russian Ruble", true),
            new("usd", "US Dollar", false)
        };

        _processingServiceMock
            .Setup(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currencies);

        // Act
        var result = await _service.IsCurrencyActiveAsync("usd");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsAmountValidForGatewayAsync Tests

    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsTrueForValidAmount()
    {
        // Arrange
        var gateways = new List<PaymentGatewayInfo>
        {
            new("sberbank", "Sberbank", "rub", true, true, 100, 100000)
        };

        _processingServiceMock
            .Setup(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        // Act
        var result = await _service.IsAmountValidForGatewayAsync("sberbank", 5000);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsFalseForAmountBelowMin()
    {
        // Arrange
        var gateways = new List<PaymentGatewayInfo>
        {
            new("sberbank", "Sberbank", "rub", true, true, 100, 100000)
        };

        _processingServiceMock
            .Setup(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        // Act
        var result = await _service.IsAmountValidForGatewayAsync("sberbank", 50);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsFalseForAmountAboveMax()
    {
        // Arrange
        var gateways = new List<PaymentGatewayInfo>
        {
            new("sberbank", "Sberbank", "rub", true, true, 100, 100000)
        };

        _processingServiceMock
            .Setup(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        // Act
        var result = await _service.IsAmountValidForGatewayAsync("sberbank", 150000);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAmountValidForGatewayAsync_ReturnsFalseForUnknownGateway()
    {
        // Arrange
        var gateways = new List<PaymentGatewayInfo>
        {
            new("sberbank", "Sberbank", "rub", true, true, 100, 100000)
        };

        _processingServiceMock
            .Setup(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(gateways);

        // Act
        var result = await _service.IsAmountValidForGatewayAsync("unknown", 5000);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public async Task ClearCache_ClearsAllCaches()
    {
        // Arrange
        var currencies = new List<CurrencyInfo> { new("rub", "Russian Ruble", true) };
        var gateways = new List<PaymentGatewayInfo> { new("sberbank", "Sberbank", "rub", true, true, 100, 100000) };

        _processingServiceMock.Setup(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currencies);
        _processingServiceMock.Setup(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>())).ReturnsAsync(gateways);

        await _service.GetAvailableCurrenciesAsync();
        await _service.GetPaymentGatewaysAsync();

        // Act
        _service.ClearCache();
        await _service.GetAvailableCurrenciesAsync();
        await _service.GetPaymentGatewaysAsync();

        // Assert - should call API twice for each (before and after clear)
        _processingServiceMock.Verify(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _processingServiceMock.Verify(x => x.GetPaymentGatewaysAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion
}

