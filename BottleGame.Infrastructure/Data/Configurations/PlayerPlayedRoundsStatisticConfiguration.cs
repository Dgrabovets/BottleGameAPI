using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the PlayerPlayedRoundsStatisticForWithdrawal entity
    /// </summary>
    public class PlayerPlayedRoundsStatisticConfiguration : IEntityTypeConfiguration<PlayerPlayedRoundsStatisticForWithdrawal>
    {
        /// <summary>
        /// Configures the PlayerPlayedRoundsStatisticForWithdrawal entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<PlayerPlayedRoundsStatisticForWithdrawal> builder)
        {
            builder.ToTable("PlayersPlayedRoundsStatisticsForWithdrawals");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(e => e.PlayerId)
                .HasColumnName("PlayerId")
                .IsRequired();

            builder.Property(e => e.DepositTransactionId)
                .HasColumnName("DepositTransactionId")
                .IsRequired(false);

            builder.Property(e => e.PlayedRoundsCount)
                .HasColumnName("PlayedRoundsCount")
                .IsRequired();

            builder.HasOne(e => e.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.MoneyTransaction)
                .WithMany()
                .HasForeignKey(e => e.DepositTransactionId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        }
    }
}
