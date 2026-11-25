using BottleGame.Application.Common;
using FluentAssertions;

namespace BottleGame.UnitTests.Common;

/// <summary>
/// Tests for LogSanitizer
/// </summary>
public class LogSanitizerTests
{
    /// <summary>
    /// Every CR and LF is removed, so the value stays on one log line
    /// </summary>
    [Theory]
    [InlineData("admin\r\n[Webhook] Fake entry", "admin[Webhook] Fake entry")]
    [InlineData("a\nb\rc", "abc")]
    [InlineData("\r\n", "")]
    public void Clean_LineBreaks_AreRemoved(string value, string expected)
    {
        LogSanitizer.Clean(value).Should().Be(expected);
    }

    /// <summary>
    /// Text without line breaks is left as it is
    /// </summary>
    [Fact]
    public void Clean_SingleLine_IsUnchanged()
    {
        LogSanitizer.Clean("sberbank card").Should().Be("sberbank card");
    }

    /// <summary>
    /// A missing value stays missing
    /// </summary>
    [Fact]
    public void Clean_Null_ReturnsNull()
    {
        LogSanitizer.Clean(null).Should().BeNull();
    }
}
