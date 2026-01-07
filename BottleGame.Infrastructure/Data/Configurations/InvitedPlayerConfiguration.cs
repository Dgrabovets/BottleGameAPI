using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности InvitedPlayer
    /// </summary>
    public class InvitedPlayerConfiguration : IEntityTypeConfiguration<InvitedPlayer>
    {
        public void Configure(EntityTypeBuilder<InvitedPlayer> builder)
        {
            builder.ToTable("InvitedPlayers");
            builder.HasKey(ip => new { ip.PlayerIdInvited, ip.PlayerIdInvitedBy });

            builder.HasOne(ip => ip.PlayerInvited)
                .WithMany(p => p.InvitedByMe)
                .HasForeignKey(ip => ip.PlayerIdInvitedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ip => ip.PlayerInvitedBy)
                .WithOne(p => p.InvitedMe)
                .HasForeignKey<InvitedPlayer>(ip => ip.PlayerIdInvited)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

