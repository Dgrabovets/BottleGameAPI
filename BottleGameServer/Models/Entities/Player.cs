using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Данные о игроке\пользователе
    /// </summary>
    [Table("Players")]
    [PrimaryKey(nameof(Id))]
    public class Player
    {
        /// <summary>
        /// Идентификатор игрока
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор пользователя в телеграм
        /// </summary>
        [Required]
        [Column("TelegramId")]
        public long TelegramId { get; set; }

        /// <summary>
        /// Имя игрока
        /// </summary>
        [Required]
        [Column("Name")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Ссылка на аватарку 
        /// </summary>
        [Required]
        [Column("AvatarUrl")]
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Дата регистрации в приложении
        /// </summary>
        [Required]
        [Column("RegisteredInAppAt")]
        public DateTime RegisteredInAppAt { get; set; }

        /// <summary>
        /// Вычисляемое свойство коэффициента побед (не хранится в БД)
        /// </summary>
        [NotMapped]
        public double WinRate
        {
            get
            {
                if (GameRounds == null || !GameRounds.Any())
                    return 0;

                int totalRounds = GameRounds.Count;
                int wins = GameRounds.Count(r => r.IsWin);
                return Math.Round((double)wins / totalRounds, 2);
            }
        }


        // Навигационные свойства
        public virtual Ban Ban { get; set; } = null!;
        public virtual ICollection<BanHistoryEntity> BansHistory { get; set; } = new List<BanHistoryEntity>();
        public virtual PlayerOptions PlayerOptions { get; set; } = null!;
        public virtual Balance Balance { get; set; } = null!;
        public virtual ICollection<GameRound> GameRounds { get; set; } = new List<GameRound>();
        public virtual ReferalLink ReferalLink { get; set; } = null!;
        public virtual ICollection<InvitedPlayer> InvitedByMe { get; set; } = new List<InvitedPlayer>();
        public virtual InvitedPlayer InvitedMe { get; set; } = null!;
        public virtual ICollection<MoneyTransaction> MoneyTransactions { get; set; } = new List<MoneyTransaction>();
    }
}
