using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Конфигурация сущности GameParams
    /// </summary>
    public class GameParamsConfiguration : IEntityTypeConfiguration<GameParams>
    {
        public void Configure(EntityTypeBuilder<GameParams> builder)
        {
            builder.ToTable("GameParams");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        }
    }
}
