using BottleGame.Application.Common;
using FluentAssertions;

namespace BottleGame.UnitTests.Common;

public class PlayerSearchQueryTests
{
    [Theory]
    [InlineData("625593041", 625593041L, null)]
    [InlineData("@player_name", null, "player_name")]
    [InlineData("  Nick  ", null, "Nick")]
    public void Resolve_FromQ_ParsesTelegramIdOrName(
        string q,
        long? expectedTelegramId,
        string? expectedName)
    {
        var result = PlayerSearchQuery.Resolve(null, null, q);

        result.TelegramId.Should().Be(expectedTelegramId);
        result.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Resolve_ExplicitTelegramId_HasPriorityOverQ()
    {
        var result = PlayerSearchQuery.Resolve(42, null, "999");

        result.TelegramId.Should().Be(42);
        result.Name.Should().BeNull();
    }
}
