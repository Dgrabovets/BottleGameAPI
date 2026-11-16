using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the ReferralLink entity
    /// </summary>
    public class ReferralLinkConfiguration : IEntityTypeConfiguration<ReferralLink>
    {
        /// <summary>
        /// Configures the ReferralLink entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<ReferralLink> builder)
        {
            builder.ToTable("ReferralLinks");
            builder.HasKey(b => b.PlayerId);
            builder.Property(b => b.PlayerId).HasColumnName("PlayerId").IsRequired();
            builder.Property(b => b.Link)
                .HasColumnName("Link")
                .HasColumnType("varchar")
                .IsRequired();

            builder.HasOne(b => b.Player)
                .WithOne(p => p.ReferralLink)
                .HasForeignKey<ReferralLink>(b => b.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

