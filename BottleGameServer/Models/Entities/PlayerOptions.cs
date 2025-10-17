using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGameServer.Models.Entities
{
    /// <summary>
    /// Сущность Настроек для игрока
    /// </summary>
    [Table("PlayerOptions")]
    public class PlayerOptions
    {
        /// <summary>
        /// Идентификатор игрока
        /// </summary>
        [ForeignKey("Player")]
        [Column("PlayerId")]
        [Required]
        public int PlayerId { get; set; }
        public virtual Player Player { get; set; } = new Player();

        /// <summary>
        /// Шанс выигрышка игрока
        /// </summary>
        [Column("WinChance")]
        [Range(0, 1)]
        [Required]
        public float WinChance { get; set; }

        /// <summary>
        /// Предпочитаемая валюта игрока (RUB, USD, KZT)
        /// </summary>
        [Column("Currency")]
        [Required]
        public CurrencyType Currency { get; set; }

        /// <summary>
        /// Кошелёк или номер карты игрока для вывода средств
        /// </summary>
        [Column("Wallet")]
        [Required]
        [MaxLength(50)]
        public string Wallet { get; set; } = string.Empty;

        /// <summary>
        /// Предпочитаемый метод выплаты игрока
        /// </summary>
        [Column("PayoutMethod")]
        [Required]
        public PayoutMethodType PayoutMethod { get; set; }

        /// <summary>
        /// Предпочитаемый метод платежа игрока
        /// </summary>
        [Column("PaymentMethod")]
        [Required]
        public PaymentMethodType PaymentMethod { get; set; }
    }
}
