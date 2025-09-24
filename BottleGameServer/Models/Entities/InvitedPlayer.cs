using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Данные о приглашенном пользователе
    /// </summary>
    [Table("InvitedPlayers")]
    public class InvitedPlayer
    {
        /// <summary>
        /// Идентификатор приглашенного игрока
        /// </summary>
        [ForeignKey("InvitedByPlayer")]
        [Column("PlayerInvitedBy")]
        [Required]
        public int PlayerIdInvitedBy { get; set; }

        /// <summary>
        /// Приглашенный игрок
        /// </summary>
        public virtual Player PlayerInvitedBy { get; set; } = null!;

        /// <summary>
        /// Идентификатор игрока, который пригласил
        /// </summary>
        [ForeignKey("InvitedPlayer")]
        [Column("PlayerIdInvited")]
        [Required]
        public int PlayerIdInvited { get; set; }

        /// <summary>
        /// Игрок, который пригласил
        /// </summary>
        public virtual Player PlayerInvited { get; set; } = null!;

        /// <summary>
        /// Дата приглашения
        /// </summary>
        [Column("InvitedAt")]
        [Required]
        public DateTime InvitedAt { get; set; }
    }
}
