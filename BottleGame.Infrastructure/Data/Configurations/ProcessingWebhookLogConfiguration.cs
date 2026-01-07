using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности ProcessingWebhookLog.
    /// </summary>
    public class ProcessingWebhookLogConfiguration : IEntityTypeConfiguration<ProcessingWebhookLog>
    {
        /// <summary>
        /// Настраивает сущность ProcessingWebhookLog.
        /// </summary>
        /// <param name="builder">Построитель типа сущности.</param>
        public void Configure(EntityTypeBuilder<ProcessingWebhookLog> builder)
        {
            builder.ToTable("ProcessingWebhookLogs");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(l => l.RawRequest)
                .HasColumnName("RawRequest")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(l => l.HttpMethod)
                .HasColumnName("HttpMethod")
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(l => l.ContentType)
                .HasColumnName("ContentType")
                .HasMaxLength(100);

            builder.Property(l => l.IpAddress)
                .HasColumnName("IpAddress")
                .HasMaxLength(45);

            builder.Property(l => l.UserAgent)
                .HasColumnName("UserAgent")
                .HasMaxLength(500);

            builder.Property(l => l.IsSignatureValid)
                .HasColumnName("IsSignatureValid")
                .IsRequired();

            builder.Property(l => l.ProcessingStatus)
                .HasColumnName("ProcessingStatus")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(l => l.ErrorMessage)
                .HasColumnName("ErrorMessage")
                .HasColumnType("text");

            builder.Property(l => l.TransactionId)
                .HasColumnName("TransactionId")
                .HasMaxLength(100);

            builder.Property(l => l.WebhookType)
                .HasColumnName("WebhookType")
                .HasMaxLength(50);

            builder.Property(l => l.Result)
                .HasColumnName("Result")
                .HasMaxLength(100);

            builder.Property(l => l.OrderId)
                .HasColumnName("OrderId")
                .HasMaxLength(100);

            builder.Property(l => l.MerchantId)
                .HasColumnName("MerchantId")
                .HasMaxLength(100);

            builder.Property(l => l.ReceivedAt)
                .HasColumnName("ReceivedAt")
                .IsRequired();

            builder.Property(l => l.ProcessedAt)
                .HasColumnName("ProcessedAt");

            // Индексы для быстрого поиска
            builder.HasIndex(l => l.TransactionId);
            builder.HasIndex(l => l.OrderId);
            builder.HasIndex(l => l.ReceivedAt);
        }
    }
}


