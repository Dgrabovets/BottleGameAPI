using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the Ban entity
    /// </summary>
    public class BanConfiguration : IEntityTypeConfiguration<Ban>
    {
        /// <summary>
        /// Configures the Ban entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<Ban> builder)
        {
            builder.ToTable("Bans");
            builder.HasKey(b => b.PlayerId);
            builder.Property(b => b.PlayerId).HasColumnName("PlayerId").IsRequired();

            builder.Property(b => b.BanReason)
                .HasColumnName("BanReason")
                .HasColumnType("varchar")
                .IsRequired();

            builder.Property(b => b.BannedAt).HasColumnName("BannedAt").IsRequired();
            builder.Property(b => b.IsActive).HasColumnName("IsActive").IsRequired();

            builder.HasOne(b => b.Player)
                .WithOne(p => p.Ban)
                .HasForeignKey<Ban>(b => b.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

