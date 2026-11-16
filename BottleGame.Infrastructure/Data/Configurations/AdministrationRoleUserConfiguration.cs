using BottleGame.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BottleGame.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration of the AdministrationRoleUser entity
    /// </summary>
    public class AdministrationRoleUserConfiguration : IEntityTypeConfiguration<AdministrationRoleUser>
    {
        /// <summary>
        /// Configures the AdministrationRoleUser entity
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public void Configure(EntityTypeBuilder<AdministrationRoleUser> builder)
        {
            builder.ToTable("AdminCreds");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(a => a.Login)
                .HasColumnName("Login")
                .HasColumnType("varchar")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(a => a.PasswordHash)
                .HasColumnName("PasswordHash")
                .HasColumnType("varchar")
                .IsRequired();

            builder.Property(a => a.Salt)
                .HasColumnName("Salt")
                .HasColumnType("varchar");

            builder.Property(a => a.CreatedAt)
                .HasColumnName("CreatedAt");
        }
    }
}

