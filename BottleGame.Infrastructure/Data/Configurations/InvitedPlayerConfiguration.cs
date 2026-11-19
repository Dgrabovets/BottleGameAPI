using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the InvitedPlayer entity
    /// </summary>
    public class InvitedPlayerConfiguration : IEntityTypeConfiguration<InvitedPlayer>
    {
        /// <summary>
        /// Configures the InvitedPlayer entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<InvitedPlayer> builder)
        {
            builder.ToTable("InvitedPlayers");
            builder.HasKey(ip => new { ip.PlayerIdInvited, ip.PlayerIdInvitedBy });

            builder.HasOne(ip => ip.Inviter)
                .WithMany(p => p.InvitedByMe)
                .HasForeignKey(ip => ip.PlayerIdInvitedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ip => ip.Invitee)
                .WithOne(p => p.InvitedMe)
                .HasForeignKey<InvitedPlayer>(ip => ip.PlayerIdInvited)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
