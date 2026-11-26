using BottleGame.API.Controllers;
using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace BottleGame.UnitTests.Controllers;

/// <summary>
/// A refusal answers 403 with its reason. Forbid(text) would read the text as an authentication scheme and fail
/// </summary>
public class ForbiddenResponseTests
{
    /// <summary>
    /// Builds a controller context for a signed-in player
    /// </summary>
    /// <param name="playerId">Player id</param>
    /// <returns>Controller context</returns>
    private static ControllerContext PlayerContext(int playerId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, playerId.ToString()), new Claim(ClaimTypes.Role, RoleNames.Player)],
                "Test"))
        }
    };

    /// <summary>
    /// A forbidden service result becomes 403 with the message
    /// </summary>
    [Fact]
    public async Task ServiceResultForbidden_Returns403WithMessage()
    {
        var service = new Mock<IGameRoundService>();
        service
            .Setup(x => x.FinishGameRound(1, It.IsAny<FinishRoundRequest>(), 1, false))
            .ReturnsAsync(Result.Forbidden("User is banned"));
        var controller = new GameRoundController(service.Object) { ControllerContext = PlayerContext(1) };

        var response = await controller.FinishGameRound(1, new FinishRoundRequest());

        var result = response.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(403);
        result.Value.Should().Be("User is banned");
    }

    /// <summary>
    /// A banned player asking for their referral link gets 403 with the message
    /// </summary>
    [Fact]
    public async Task ReferralLinkOfBannedPlayer_Returns403WithMessage()
    {
        using var context = TestDbContextFactory.CreateWithReferenceData();
        var player = TestDbContextFactory.CreateTestPlayer(context);
        TestDbContextFactory.AddActiveBan(context, player.Id);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AppName"] = "bot/app" }).Build();
        var controller = new ReferralController(context, NullLogger<ReferralController>.Instance, config) { ControllerContext = PlayerContext(player.Id) };

        var response = await controller.GetReferralLink(player.Id);

        var result = response.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(403);
        result.Value.Should().Be("The player is banned");
    }
}
