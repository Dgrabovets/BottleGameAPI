using BottleGame.API.Extensions;
using BottleGame.Domain.Constants;
using FluentAssertions;
using System.Security.Claims;

namespace BottleGame.UnitTests.Access;

/// <summary>
/// Tests for the checks of the signed-in user
/// </summary>
public class ClaimsPrincipalExtensionsTests
{
    /// <summary>
    /// Staff see any player, a player sees only their own data
    /// </summary>
    [Theory]
    [InlineData(RoleNames.Admin, "1", true)]
    [InlineData(RoleNames.Moderator, "1", true)]
    [InlineData(RoleNames.Player, "7", true)]
    [InlineData(RoleNames.Player, "8", false)]
    [InlineData(RoleNames.Player, "not a number", false)]
    public void HasAccessToPlayer_DependsOnRoleAndId(string role, string userId, bool expected)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.NameIdentifier, userId)], "test");

        new ClaimsPrincipal(identity).HasAccessToPlayer(7).Should().Be(expected);
    }
}
