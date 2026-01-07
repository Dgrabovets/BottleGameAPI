using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности Balance
    /// </summary>
    public class BalanceConfiguration : IEntityTypeConfiguration<Balance>
    {
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

