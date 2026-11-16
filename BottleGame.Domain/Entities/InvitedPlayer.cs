using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Link between the invited and the inviting players
    /// </summary>
    [Table("InvitedPlayers")]
    public class InvitedPlayer
    {
        /// <summary>
        /// Identifier of the invited player
        /// </summary>
        [Key]
        [Column("PlayerIdInvited", Order = 0)]
        public int PlayerIdInvited { get; set; }

        /// <summary>
        /// Identifier of the inviting player
        /// </summary>
        [Key]
        [Column("PlayerIdInvitedBy", Order = 1)]
        public int PlayerIdInvitedBy { get; set; }

        /// <summary>
        /// Invitation date
        /// </summary>
        [Column("InvitedAt")]
        public DateTime InvitedAt { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Inviting player (linked through PlayerIdInvitedBy)
        /// </summary>
        public virtual Player Inviter { get; set; } = null!;

        /// <summary>
        /// Invited player (linked through PlayerIdInvited)
        /// </summary>
        public virtual Player Invitee { get; set; } = null!;

        #endregion Navigation Properties
    }
}
