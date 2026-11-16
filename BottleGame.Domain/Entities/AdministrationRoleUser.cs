using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Model of an administrative role account
    /// </summary>
    public class AdministrationRoleUser
    {
        /// <summary>
        /// User identifier
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Login
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("Login")]
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Password hash
        /// </summary>
        [Required]
        [Column("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Salt for extra security
        /// </summary>
        [Column("Salt")]
        public string Salt { get; set; } = string.Empty;

        /// <summary>
        /// Account creation date
        /// </summary>
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Role (1 - admin, 2 - moderator)
        /// </summary>
        [Column("Role")]
        public int Role { get; set; }
    }
}

