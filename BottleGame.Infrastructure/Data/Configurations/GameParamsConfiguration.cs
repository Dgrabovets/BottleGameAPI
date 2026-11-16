using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the GameParams entity
    /// </summary>
    public class GameParamsConfiguration : IEntityTypeConfiguration<GameParams>
    {
        /// <summary>
        /// Configures the GameParams entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<GameParams> builder)
        {
            builder.ToTable("GameParams");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        }
    }
}
