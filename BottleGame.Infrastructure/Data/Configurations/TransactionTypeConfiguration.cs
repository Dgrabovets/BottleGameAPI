using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the TransactionType entity
    /// </summary>
    public class TransactionTypeConfiguration : IEntityTypeConfiguration<TransactionType>
    {
        /// <summary>
        /// Configures the TransactionType entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<TransactionType> builder)
        {
            builder.ToTable("TransactionTypes");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("Id");
            builder.Property(t => t.Name)
                .HasColumnName("Name")
                .HasColumnType("varchar")
                .IsRequired();
        }
    }
}
