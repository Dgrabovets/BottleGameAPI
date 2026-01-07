using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Связь между приглашённым и пригласившим игроками
    /// </summary>
    [Table("InvitedPlayers")]
    public class InvitedPlayer
    {
        /// <summary>
        /// Идентификатор приглашённого игрока
        /// </summary>
        [Key]
        [Column("PlayerIdInvited", Order = 0)]
        public int PlayerIdInvited { get; set; }

        /// <summary>
        /// Идентификатор пригласившего игрока
        /// </summary>
        [Key]
        [Column("PlayerIdInvitedBy", Order = 1)]
        public int PlayerIdInvitedBy { get; set; }

        /// <summary>
        /// Дата приглашения
        /// </summary>
        [Column("InvitedAt")]
        public DateTime InvitedAt { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Приглашённый игрок
        /// </summary>
        public virtual Player PlayerInvited { get; set; } = null!;

        /// <summary>
        /// Пригласивший игрок
        /// </summary>
        public virtual Player PlayerInvitedBy { get; set; } = null!;

        #endregion Navigation Properties
    }
}
