using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the Balance entity
    /// </summary>
    public class BalanceConfiguration : IEntityTypeConfiguration<Balance>
    {
        /// <summary>
        /// Configures the Balance entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<Balance> builder)
        {
            builder.ToTable("PlayersBalance");
            builder.HasKey(b => b.PlayerId);
            builder.Property(b => b.PlayerId).HasColumnName("PlayerId");
            builder.Property(b => b.Amount).HasColumnName("Amount").IsRequired();

            builder.HasOne(b => b.Player)
                .WithOne(p => p.Balance)
                .HasForeignKey<Balance>(b => b.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

