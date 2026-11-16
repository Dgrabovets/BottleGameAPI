using BottleGame.Domain.Entities;
using BottleGame.UnitTests.Fixtures;
using FluentAssertions;

namespace BottleGame.UnitTests;

/// <summary>
/// Проверяет, как EF сохраняет связь «приглашённый, пригласивший»
/// </summary>
public class InvitedPlayerMappingTests
{
    [Fact]
    public void NavigationAssignment_StoresInvitedAndInviterIds()
    {
        using var context = TestDbContextFactory.CreateWithReferenceData();
        var inviter = TestDbContextFactory.CreateTestPlayer(context, telegramId: 1);
        var invited = TestDbContextFactory.CreateTestPlayer(context, telegramId: 2);

        context.InvitedPlayers.Add(new InvitedPlayer
        {
            Inviter = inviter,
            Invitee = invited,
            InvitedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var row = context.InvitedPlayers.Single();
        row.PlayerIdInvited.Should().Be(invited.Id);
        row.PlayerIdInvitedBy.Should().Be(inviter.Id);
    }
}
