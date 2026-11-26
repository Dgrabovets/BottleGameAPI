using BottleGame.API.Controllers;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Entities;
using BottleGame.Infrastructure.Data;
using BottleGame.Infrastructure.Services;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace BottleGame.UnitTests.Controllers;

/// <summary>
/// Tests for AuthorizationController: Telegram sign-in with and without an inviter, and the staff login
/// </summary>
public class AuthorizationControllerTests : IDisposable
{
    /// <summary>
    /// Telegram id of the player who signs in
    /// </summary>
    private const long NEW_TELEGRAM_ID = 777001;

    /// <summary>
    /// Database context of the test
    /// </summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Mock of the password hashing service
    /// </summary>
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

    /// <summary>
    /// The controller under test
    /// </summary>
    private readonly AuthorizationController _controller;

    /// <summary>
    /// Creates the test fixture: the database, the real initData check and the controller under test
    /// </summary>
    public AuthorizationControllerTests()
    {
        _context = TestDbContextFactory.CreateWithReferenceData();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TelegramBotToken"] = TelegramInitData.BOT_TOKEN })
            .Build();

        var tokenServiceMock = new Mock<ITokenService>();
        tokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<IEnumerable<Claim>>())).Returns("token");

        _controller = new AuthorizationController(
            tokenServiceMock.Object,
            _context,
            _passwordHasherMock.Object,
            NullLogger<AuthorizationController>.Instance,
            new TelegramAuthService(config, NullLogger<TelegramAuthService>.Instance),
            new Mock<IPasswordUpgradeService>().Object);
    }

    /// <summary>
    /// Releases the database context used by the tests
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helpers

    /// <summary>
    /// Gives the player a referral link, so others can name them as the inviter
    /// </summary>
    /// <param name="playerId">Player id</param>
    private void AddReferralLink(int playerId)
    {
        _context.ReferralLinks.Add(new ReferralLink { PlayerId = playerId, Link = $"https://t.me/app?startapp=ref_{playerId}" });
        _context.SaveChanges();
    }

    /// <summary>
    /// The player created by the sign-in, with the rows a complete account has
    /// </summary>
    /// <returns>The player or null if none was created</returns>
    private Player? NewPlayer() => _context.Players
        .AsNoTracking()
        .Include(p => p.Balance)
        .Include(p => p.PlayerOptions)
        .FirstOrDefault(p => p.TelegramId == NEW_TELEGRAM_ID);

    #endregion Helpers

    #region Telegram Sign-in Tests

    /// <summary>
    /// A signed start_param that names a player without a referral link signs in without an invitation
    /// </summary>
    [Fact]
    public async Task TelegramLogin_InviterWithoutReferralLink_SignsInWithoutInvitation()
    {
        var inviter = TestDbContextFactory.CreateTestPlayer(_context);

        var response = await _controller.TelegramLogin(TelegramInitData.Signed(NEW_TELEGRAM_ID, $"ref_{inviter.Id}"));

        response.Result.Should().BeOfType<OkObjectResult>();
        NewPlayer()!.Balance.Amount.Should().Be(0);
        _context.InvitedPlayers.Should().BeEmpty();
    }

    /// <summary>
    /// An inviter that does not exist does not stop the sign-in and does not leave a half-created account
    /// </summary>
    [Fact]
    public async Task TelegramLogin_UnknownInviter_CreatesCompleteAccountWithoutInvitation()
    {
        AddReferralLink(999);

        var response = await _controller.TelegramLogin(TelegramInitData.Signed(NEW_TELEGRAM_ID, "ref_999"));

        response.Result.Should().BeOfType<OkObjectResult>();
        var player = NewPlayer();
        player.Should().NotBeNull();
        player!.Balance.Should().NotBeNull();
        player.PlayerOptions.Should().NotBeNull();
        _context.PlayersPlayedRoundsStatisticForWithdrawals.Should().Contain(s => s.PlayerId == player.Id);
        _context.InvitedPlayers.Should().BeEmpty();
    }

    /// <summary>
    /// A known inviter with a referral link gets the invitation and the new player gets the start bonus
    /// </summary>
    [Fact]
    public async Task TelegramLogin_InviterWithReferralLink_RecordsInvitation()
    {
        var inviter = TestDbContextFactory.CreateTestPlayer(_context);
        AddReferralLink(inviter.Id);

        var response = await _controller.TelegramLogin(TelegramInitData.Signed(NEW_TELEGRAM_ID, $"ref_{inviter.Id}"));

        response.Result.Should().BeOfType<OkObjectResult>();
        var player = NewPlayer()!;
        player.Balance.Amount.Should().Be(100);
        _context.InvitedPlayers.Should().ContainSingle(i => i.PlayerIdInvited == player.Id && i.PlayerIdInvitedBy == inviter.Id);
    }

    /// <summary>
    /// Data with a broken signature is refused before anything is created
    /// </summary>
    [Fact]
    public async Task TelegramLogin_TamperedData_ReturnsUnauthorizedWithoutCreatingPlayer()
    {
        var tampered = TelegramInitData.Signed(NEW_TELEGRAM_ID).Replace("Demo", "Evil");

        var response = await _controller.TelegramLogin(tampered);

        response.Result.Should().BeOfType<UnauthorizedObjectResult>();
        NewPlayer().Should().BeNull();
    }

    /// <summary>
    /// A banned player gets 403 with the reason, not an error from a missing authentication scheme
    /// </summary>
    [Fact]
    public async Task TelegramLogin_BannedPlayer_Returns403WithMessage()
    {
        var player = TestDbContextFactory.CreateTestPlayer(_context, NEW_TELEGRAM_ID);
        TestDbContextFactory.AddActiveBan(_context, player.Id);

        var response = await _controller.TelegramLogin(TelegramInitData.Signed(NEW_TELEGRAM_ID));

        var result = response.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(403);
        result.Value.Should().Be("The player is banned");
    }

    #endregion Telegram Sign-in Tests

    #region Staff Login Tests

    /// <summary>
    /// An unknown login still costs one password hash, so the response time does not tell which logins exist
    /// </summary>
    [Fact]
    public async Task LoginAdmin_UnknownLogin_DoesTheSameHashWork()
    {
        var response = await _controller.LoginAdmin(new AdministrationRoleUserCredentials { Login = "nobody@example.com", Password = "password-1" });

        response.Result.Should().BeOfType<UnauthorizedObjectResult>();
        _passwordHasherMock.Invocations.Should().ContainSingle();
    }

    #endregion Staff Login Tests
}
