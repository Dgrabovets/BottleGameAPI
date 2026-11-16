using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the TransactionStatus entity
    /// </summary>
    public class TransactionStatusConfiguration : IEntityTypeConfiguration<TransactionStatus>
    {
        /// <summary>
        /// Configures the TransactionStatus entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
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

