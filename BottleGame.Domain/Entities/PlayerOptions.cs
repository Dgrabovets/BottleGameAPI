using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Настройки игрока
    /// </summary>
    [Table("PlayersOptions")]
    public class PlayerOptions
    {
        /// <summary>
        /// Идентификатор игрока (первичный ключ)
        /// </summary>
        [Key]
        [ForeignKey("Player")]
        [Column("PlayerId")]
        public int PlayerId { get; set; }

        /// <summary>
        /// Персональный шанс выигрыша
        /// </summary>
        [Required]
        [Column("WinChance")]
        public float WinChance { get; set; }

        /// <summary>
        /// Кошелёк для выплат
        /// </summary>
        [Column("Wallet")]
        [MaxLength(100)]
        public string? Wallet { get; set; }

        /// <summary>
        /// Код валюты (например, "rub", "kzt").
        /// Значения загружаются динамически из процессинга.
        /// </summary>
        [Column("Currency")]
        [MaxLength(10)]
        public string? Currency { get; set; }

        /// <summary>
        /// Код метода выплаты (например, "sbp_rub", "bankcard_rub").
        /// Значения загружаются динамически из процессинга.
        /// </summary>
        [Required]
        [Column("PayoutMethod")]
        [MaxLength(50)]
        public string PayoutMethod { get; set; } = string.Empty;

        /// <summary>
        /// Код метода оплаты (например, "sbp_rub", "bankcard_rub").
        /// Значения загружаются динамически из процессинга.
        /// </summary>
        [Required]
        [Column("PaymentMethod")]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Тип платёжных реквизитов (например, "card", "phone", "sbp").
        /// Доступные значения зависят от выбранного PaymentMethod.
        /// </summary>
        [Required]
        [Column("PaymentDetailType")]
        [MaxLength(50)]
        public string PaymentDetailType { get; set; } = "card";

        #region Navigation Properties

        /// <summary>
        /// Связанный игрок
        /// </summary>
        public virtual Player Player { get; set; } = null!;

        #endregion Navigation Properties
    }
}
