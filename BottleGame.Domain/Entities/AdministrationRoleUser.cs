using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Модель учетной записи административной роли
    /// </summary>
    public class AdministrationRoleUser
    {
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Логин
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("Login")]
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Хэш пароля
        /// </summary>
        [Required]
        [Column("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Соль для дополнительной безопасности
        /// </summary>
        [Column("Salt")]
        public string Salt { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания учетной записи
        /// </summary>
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Роль (1 - админ, 2 - модератор)
        /// </summary>
        [Column("Role")]
        public int Role { get; set; }
    }
}

