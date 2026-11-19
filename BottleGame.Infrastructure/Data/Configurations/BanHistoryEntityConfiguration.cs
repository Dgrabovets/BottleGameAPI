using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the BanHistoryEntity entity
    /// </summary>
    public class BanHistoryEntityConfiguration : IEntityTypeConfiguration<BanHistoryEntity>
    {
        /// <summary>
        /// Configures the BanHistoryEntity entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<BanHistoryEntity> builder)
        {
            builder.ToTable("BansHistory");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.PlayerId).HasColumnName("PlayerId").IsRequired();

            builder.Property(b => b.BanReason)
                .HasColumnName("BanReason")
                .HasColumnType("varchar")
                .IsRequired();

            builder.Property(b => b.BannedAt).HasColumnName("BannedAt").IsRequired();
            builder.Property(b => b.UnbannedAt).HasColumnName("UnbannedAt");

            builder.HasOne(b => b.Player)
                .WithMany(p => p.BansHistory)
                .HasForeignKey(b => b.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
