using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности PlayerOptions
    /// </summary>
    public class PlayerOptionsConfiguration : IEntityTypeConfiguration<PlayerOptions>
    {
        public void Configure(EntityTypeBuilder<PlayerOptions> builder)
        {
            builder.ToTable("PlayersOptions");
            builder.HasKey(e => e.PlayerId);
            builder.Property(e => e.WinChance).HasColumnName("WinChance").IsRequired();

            builder.Property(e => e.Wallet).HasColumnName("Wallet");

            builder.Property(e => e.Currency)
                .HasColumnName("Currency")
                .HasConversion<string>()
                .HasColumnType("varchar");

            builder.Property(e => e.PayoutMethod)
                .HasColumnName("PayoutMethod")
                .HasConversion<string>()
                .HasColumnType("varchar")
                .IsRequired();

            builder.Property(e => e.PaymentMethod)
                .HasColumnName("PaymentMethod")
                .HasConversion<string>()
                .HasColumnType("varchar")
                .IsRequired();

            builder.HasOne(b => b.Player)
                .WithOne(p => p.PlayerOptions)
                .HasForeignKey<PlayerOptions>(b => b.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

