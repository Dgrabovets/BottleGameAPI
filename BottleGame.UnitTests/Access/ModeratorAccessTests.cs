using BottleGame.API.Controllers;
using BottleGame.Domain.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace BottleGame.UnitTests.Access;

/// <summary>
/// Tests that pin down which endpoints each role may call, so a role cannot grow by accident
/// </summary>
public class ModeratorAccessTests
{
    /// <summary>
    /// Roles that may call every action, read from the Authorize attribute of the action or of its controller
    /// </summary>
    private static Dictionary<string, string[]> RolesByAction()
    {
        var controllers = typeof(BaseApiController).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));
        var result = new Dictionary<string, string[]>();

        foreach (var controller in controllers)
        {
            var classRoles = controller.GetCustomAttribute<AuthorizeAttribute>()?.Roles;

            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any()))
            {
                var anonymous = action.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
                var roles = action.GetCustomAttribute<AuthorizeAttribute>()?.Roles ?? classRoles;
                var key = $"{controller.Name.Replace("Controller", string.Empty)}.{action.Name}";

                result[key] = anonymous || roles is null ? [] : roles.Split(',', StringSplitOptions.TrimEntries);
            }
        }

        return result;
    }

    /// <summary>
    /// A moderator may read players and transactions, approve or reject withdrawals and ban or unban players, and nothing else
    /// </summary>
    [Fact]
    public void ModeratorMayCallOnlyTheModerationEndpoints()
    {
        var moderatorActions = RolesByAction()
            .Where(pair => pair.Value.Contains(RoleNames.Moderator))
            .Select(pair => pair.Key);

        moderatorActions.Should().BeEquivalentTo(
            "Balance.GetAllTransactionStatuses",
            "Balance.GetAllTransactionTypes",
            "Balance.GetAllTransactions",
            "Balance.GetPlayerTransactions",
            "Balance.UpdateTransactionStatus",
            "Player.BanPlayer",
            "Player.GetAllPlayers",
            "Player.GetAllPlayersRoundsStatisticsForWithdrawal",
            "Player.GetPlayer",
            "Player.GetTop100Players",
            "Player.IsBanned",
            "Player.UnbanPlayer");
    }

    /// <summary>
    /// Statistics, game settings, the application balance and the staff list stay for administrators
    /// </summary>
    [Theory]
    [InlineData("Administration.GetStatistics")]
    [InlineData("Administration.UpdateAppBalanceThreshold")]
    [InlineData("Administration.TopUpAppBalance")]
    [InlineData("Administration.GetModerators")]
    [InlineData("Administration.RegisterModerator")]
    [InlineData("GameParams.UpdateGameParams")]
    [InlineData("Player.EditPlayer")]
    public void AdministrativeEndpointsStayAdminOnly(string action)
    {
        RolesByAction().Should().ContainKey(action);
        RolesByAction()[action].Should().NotContain(RoleNames.Moderator);
    }
}
