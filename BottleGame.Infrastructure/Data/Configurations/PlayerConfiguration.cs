using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности Player
    /// </summary>
    public class PlayerConfiguration : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> builder)
        {
            builder.ToTable("Players");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(p => p.TelegramId).HasColumnName("TelegramId").IsRequired();
            builder.Property(p => p.Name)
                .HasColumnName("Name")
                .HasColumnType("varchar")
                .HasMaxLength(255)
                .IsRequired();
            builder.Property(p => p.AvatarUrl)
                .HasColumnName("AvatarUrl")
                .HasColumnType("varchar")
                .IsRequired();
            builder.Property(p => p.RegisteredInAppAt).HasColumnName("RegisteredInAppAt").IsRequired();
        }
    }
}

