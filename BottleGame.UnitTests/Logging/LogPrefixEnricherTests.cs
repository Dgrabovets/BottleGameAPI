using BottleGame.API.Logging;
using BottleGame.Infrastructure.External.YooKassa;
using BottleGame.Infrastructure.Services.Payments;
using FluentAssertions;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BottleGame.UnitTests.Logging;

/// <summary>
/// Tests for LogPrefixEnricher
/// </summary>
public class LogPrefixEnricherTests
{
    /// <summary>
    /// Writes one entry through the enricher and returns its prefix
    /// </summary>
    /// <param name="source">Source context of the entry, null for an entry without one</param>
    /// <returns>Value of the Prefix property</returns>
    private static object? PrefixOf(string? source)
    {
        LogEvent? written = null;
        var sink = new Mock<ILogEventSink>();
        sink.Setup(x => x.Emit(It.IsAny<LogEvent>())).Callback<LogEvent>(logEvent => written = logEvent);

        using var logger = new LoggerConfiguration().Enrich.With(new LogPrefixEnricher()).WriteTo.Sink(sink.Object).CreateLogger();
        var scoped = source == null ? logger : logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, source);
        scoped.Information("Entry");

        return ((ScalarValue)written!.Properties["Prefix"]).Value;
    }

    /// <summary>
    /// The text of the attribute replaces the class name
    /// </summary>
    [Fact]
    public void Enrich_ClassWithAttribute_UsesItsText()
    {
        PrefixOf(typeof(YooKassaPaymentProvider).FullName).Should().Be("[YooKassa] ");
    }

    /// <summary>
    /// Without the attribute the class name is split into words
    /// </summary>
    [Fact]
    public void Enrich_ClassWithoutAttribute_SplitsItsName()
    {
        PrefixOf(typeof(LiveTransactionProcessor).FullName).Should().Be("[Live transaction processor] ");
    }

    /// <summary>
    /// Framework entries, unknown types, malformed names and entries without a source get an empty prefix
    /// </summary>
    /// <param name="source">Source context of the entry</param>
    [Theory]
    [InlineData("Microsoft.AspNetCore.Hosting.Diagnostics")]
    [InlineData("BottleGame.Infrastructure.Services.NoSuchService")]
    [InlineData("BottleGame.[[")]
    [InlineData("")]
    [InlineData(null)]
    public void Enrich_NotASolutionType_AddsEmptyPrefix(string? source)
    {
        PrefixOf(source).Should().Be(string.Empty);
    }
}
