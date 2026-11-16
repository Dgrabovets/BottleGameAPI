using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Player/user entity
    /// </summary>
    [Table("Players")]
    [PrimaryKey(nameof(Id))]
    public class Player
    {
        /// <summary>
        /// Player id
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Telegram user identifier
        /// </summary>
        [Required]
        [Column("TelegramId")]
        public long TelegramId { get; set; }

        /// <summary>
        /// Player name
        /// </summary>
        [Required]
        [Column("Name")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Link to the avatar
        /// </summary>
        [Required]
        [Column("AvatarUrl")]
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Date of registration in the application
        /// </summary>
        [Required]
        [Column("RegisteredInAppAt")]
        public DateTime RegisteredInAppAt { get; set; }

        /// <summary>
        /// Calculated win percentage of the player (not stored in the database)
        /// </summary>
        [NotMapped]
        public double WinRate
        {
            get
            {
                if (GameRounds is null || !GameRounds.Any())
                    return 0;

                int totalRounds = GameRounds.Count;
                int wins = GameRounds.Count(r => r.IsWin);
                return Math.Round((double)wins / totalRounds, 2);
            }
        }

        #region Navigation Properties

        /// <summary>
        /// Ban information of the player
        /// </summary>
        public virtual Ban Ban { get; set; } = null!;

        /// <summary>
        /// Ban history of the player
        /// </summary>
        public virtual ICollection<BanHistoryEntity> BansHistory { get; set; } = new List<BanHistoryEntity>();

        /// <summary>
        /// Player settings
        /// </summary>
        public virtual PlayerOptions PlayerOptions { get; set; } = null!;

        /// <summary>
        /// Player balance
        /// </summary>
        public virtual Balance Balance { get; set; } = null!;

        /// <summary>
        /// Game rounds of the player
        /// </summary>
        public virtual ICollection<GameRound> GameRounds { get; set; } = new List<GameRound>();

        /// <summary>
        /// Referral link of the player
        /// </summary>
        public virtual ReferralLink ReferralLink { get; set; } = null!;

        /// <summary>
        /// Players invited by this player
        /// </summary>
        public virtual ICollection<InvitedPlayer> InvitedByMe { get; set; } = new List<InvitedPlayer>();

        /// <summary>
        /// Player who invited this player
        /// </summary>
        public virtual InvitedPlayer InvitedMe { get; set; } = null!;

        /// <summary>
        /// Money transactions of the player
        /// </summary>
        public virtual ICollection<MoneyTransaction> MoneyTransactions { get; set; } = new List<MoneyTransaction>();

        #endregion Navigation Properties
    }
}
