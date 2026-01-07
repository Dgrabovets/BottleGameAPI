using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности TransactionStatus
    /// </summary>
    public class TransactionStatusConfiguration : IEntityTypeConfiguration<TransactionStatus>
    {
        public void Configure(EntityTypeBuilder<TransactionStatus> builder)
        {
            builder.ToTable("TransactionStatuses");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasColumnName("Id");
            builder.Property(s => s.Name)
                .HasColumnName("Name")
                .HasColumnType("varchar")
                .IsRequired();
        }
    }
}

