using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the MoneyTransaction entity
    /// </summary>
    public class MoneyTransactionConfiguration : IEntityTypeConfiguration<MoneyTransaction>
    {
        /// <summary>
        /// Configures the MoneyTransaction entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<MoneyTransaction> builder)
        {
            builder.ToTable("MoneyTransactions");
            builder.HasKey(t => t.Id);

            // Main fields
            builder.Property(t => t.PlayerId)
                .HasColumnName("PlayerId");

            // Two concurrent handlers of the same transaction must not both apply it.
            builder.Property(t => t.Version)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.Property(t => t.Amount)
                .HasColumnName("Amount")
                .IsRequired();

            builder.Property(t => t.TransactionStatusId)
                .HasColumnName("StatusId")
                .IsRequired();

            builder.Property(t => t.TransactionTypeId)
                .HasColumnName("TransactionTypeId")
                .IsRequired();

            // Processing fields
            builder.Property(t => t.ProcessingOrderId)
                .HasColumnName("ProcessingOrderId")
                .HasMaxLength(100);

            builder.Property(t => t.PaymentUrl)
                .HasColumnName("PaymentUrl")
                .HasMaxLength(500);

            builder.Property(t => t.PaymentGateway)
                .HasColumnName("PaymentGateway")
                .HasMaxLength(50);

            builder.Property(t => t.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(10);

            builder.Property(t => t.ExchangeRate)
                .HasColumnName("ExchangeRate");

            builder.Property(t => t.IsProcessingSuccess)
                .HasColumnName("IsProcessingSuccess");

            builder.Property(t => t.ProcessingError)
                .HasColumnName("ProcessingError")
                .HasMaxLength(500);

            // Time fields
            builder.Property(t => t.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired();

            builder.Property(t => t.ProcessedAt)
                .HasColumnName("ProcessedAt");

            // Relations
            builder.HasOne(t => t.Player)
                .WithMany(p => p.MoneyTransactions)
                .HasForeignKey(b => b.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.TransactionType)
                .WithMany(t => t.MoneyTransactions)
                .HasForeignKey(t => t.TransactionTypeId);

            builder.HasOne(t => t.TransactionStatus)
                .WithMany(s => s.MoneyTransactions)
                .HasForeignKey(t => t.TransactionStatusId);

            // Indexes
            builder.HasIndex(t => t.ProcessingOrderId);
            builder.HasIndex(t => t.CreatedAt);
        }
    }
}
