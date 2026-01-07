using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности MoneyTransaction.
    /// </summary>
    public class MoneyTransactionConfiguration : IEntityTypeConfiguration<MoneyTransaction>
    {
        /// <summary>
        /// Настраивает сущность MoneyTransaction.
        /// </summary>
        /// <param name="builder">Построитель типа сущности.</param>
        public void Configure(EntityTypeBuilder<MoneyTransaction> builder)
        {
            builder.ToTable("MoneyTransactions");
            builder.HasKey(t => t.Id);

            // Основные поля
            builder.Property(t => t.PlayerId)
                .HasColumnName("PlayerId");

            builder.Property(t => t.Amount)
                .HasColumnName("Amount")
                .IsRequired();

            builder.Property(t => t.TransactionStatusId)
                .HasColumnName("StatusId")
                .IsRequired();

            builder.Property(t => t.TransactionTypeId)
                .HasColumnName("TransactionTypeId")
                .IsRequired();

            // Поля процессинга
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

            // Временные поля
            builder.Property(t => t.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired();

            builder.Property(t => t.ProcessedAt)
                .HasColumnName("ProcessedAt");

            // Связи
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

            // Индексы
            builder.HasIndex(t => t.ProcessingOrderId);
            builder.HasIndex(t => t.CreatedAt);
        }
    }
}
