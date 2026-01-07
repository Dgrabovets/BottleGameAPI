using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности GameRound
    /// </summary>
    public class GameRoundConfiguration : IEntityTypeConfiguration<GameRound>
    {
        public void Configure(EntityTypeBuilder<GameRound> builder)
        {
            builder.ToTable("GameRounds");
            builder.HasKey(g => g.Id);
            builder.Property(g => g.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(g => g.PlayerId).HasColumnName("PlayerId").IsRequired();
            builder.Property(g => g.Bet).HasColumnName("Bet").IsRequired();
            builder.Property(g => g.IsWin).HasColumnName("IsWin").IsRequired();
            builder.Property(g => g.FinishedAt).HasColumnName("FinishedAt").IsRequired();

            builder.HasOne(g => g.Player)
                .WithMany(p => p.GameRounds)
                .HasForeignKey(g => g.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

