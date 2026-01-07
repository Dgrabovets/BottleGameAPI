using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Сущность игрока/пользователя
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
        /// Идентификатор пользователя в Telegram
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
        /// Вычисляемый процент выигрышей игрока (не хранится в БД)
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
        /// Информация о бане игрока
        /// </summary>
        public virtual Ban Ban { get; set; } = null!;

        /// <summary>
        /// История банов игрока
        /// </summary>
        public virtual ICollection<BanHistoryEntity> BansHistory { get; set; } = new List<BanHistoryEntity>();

        /// <summary>
        /// Настройки игрока
        /// </summary>
        public virtual PlayerOptions PlayerOptions { get; set; } = null!;

        /// <summary>
        /// Баланс игрока
        /// </summary>
        public virtual Balance Balance { get; set; } = null!;

        /// <summary>
        /// Игровые раунды игрока
        /// </summary>
        public virtual ICollection<GameRound> GameRounds { get; set; } = new List<GameRound>();

        /// <summary>
        /// Реферальная ссылка игрока
        /// </summary>
        public virtual ReferralLink ReferralLink { get; set; } = null!;

        /// <summary>
        /// Игроки, приглашённые этим игроком
        /// </summary>
        public virtual ICollection<InvitedPlayer> InvitedByMe { get; set; } = new List<InvitedPlayer>();

        /// <summary>
        /// Игрок, пригласивший этого игрока
        /// </summary>
        public virtual InvitedPlayer InvitedMe { get; set; } = null!;

        /// <summary>
        /// Денежные транзакции игрока
        /// </summary>
        public virtual ICollection<MoneyTransaction> MoneyTransactions { get; set; } = new List<MoneyTransaction>();

        #endregion Navigation Properties
    }
}
