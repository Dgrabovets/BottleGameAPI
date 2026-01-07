using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности AppBalance
    /// </summary>
    public class AppBalanceConfiguration : IEntityTypeConfiguration<AppBalance>
    {
        public void Configure(EntityTypeBuilder<AppBalance> builder)
        {
            builder.ToTable("AppBalance");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(b => b.Amount).HasColumnName("Amount").IsRequired();
            builder.Property(b => b.LowerThreshold).HasColumnName("LowerThreshold").IsRequired();
            builder.Property(b => b.LowerThresholdUpdatedAt).HasColumnName("LowerThresholdUpdatedAt");
            builder.Property(b => b.LowerThresholdAdminIdUpdatedBy).HasColumnName("LowerThresholdAdminIdUpdatedBy");
            builder.Property(b => b.BalanceAmountUpdatedAt).HasColumnName("BalanceAmountUpdatedAt");
            builder.Property(b => b.BalanceAmountAdminIdUpdatedBy).HasColumnName("BalanceAmountAdminIdUpdatedBy");
        }
    }
}

