using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Игровые параметры
    /// </summary>
    [Table("GameParams")]
    public class GameParams
    {
        /// <summary>
        /// Идентификатор параметров
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Базовый шанс выигрыша
        /// </summary>
        [Column("BaseWinChance")]
        [Range(0, 1)]
        [Required]
        public float BaseWinChance { get; set; }

        /// <summary>
        /// Минимальная сумма депозита
        /// </summary>
        [Column("MinDepositAmount")]
        [Range(0, int.MaxValue)]
        [Required]
        public int MinDepositAmount { get; set; }

        /// <summary>
        /// Минимальное количество раундов, необходимых для вывода средств
        /// </summary>
        [Column("MinRoundsCountForWithdrawal")]
        [Range(0, int.MaxValue)]
        [Required]
        public int MinRoundsCountForWithdrawal { get; set; }

        /// <summary>
        /// Минимальный шанс выигрыша
        /// </summary>
        [Column("MinWinChance")]
        [Range(0, 1)]
        [Required]
        public float MinWinChance { get; set; }

        /// <summary>
        /// Максимальный шанс выигрыша
        /// </summary>
        [Column("MaxWinChance")]
        [Range(0, 1)]
        [Required]
        public float MaxWinChance { get; set; }

        /// <summary>
        /// Минимальное уменьшение шанса за бросок
        /// </summary>
        [Column("ChanceDecPerThrowMin")]
        [Range(0.01, 1)]
        [Required]
        public float ChanceDecPerThrowMin { get; set; }

        /// <summary>
        /// Максимальное уменьшение шанса за бросок
        /// </summary>
        [Column("ChanceDecPerThrowMax")]
        [Range(0.01, 1)]
        [Required]
        public float ChanceDecPerThrowMax { get; set; }

        /// <summary>
        /// Уменьшение шанса за вращение
        /// </summary>
        [Column("ChanceDecPerRotation")]
        [Range(0.01, 0.2)]
        [Required]
        public float ChanceDecPerRotation { get; set; }

        /// <summary>
        /// Минимальный шанс после вращения
        /// </summary>
        [Column("MinRotationWinChance")]
        [Range(0, 0.5)]
        [Required]
        public float MinRotationWinChance { get; set; }

        /// <summary>
        /// Минимальный множитель
        /// </summary>
        [Column("MinMultiplier")]
        [Range(1, float.MaxValue)]
        [Required]
        public float MinMultiplier { get; set; }

        /// <summary>
        /// Максимальный множитель
        /// </summary>
        [Column("MaxMultiplier")]
        [Range(1, float.MaxValue)]
        [Required]
        public float MaxMultiplier { get; set; }

        /// <summary>
        /// Начальная потеря баланса
        /// </summary>
        [Column("InitialBalanceLoss")]
        [Range(0.1, 0.15)]
        [Required]
        public float InitialBalanceLoss { get; set; }

        /// <summary>
        /// Шанс отклонения
        /// </summary>
        [Column("DeviationChance")]
        [Range(0, 1)]
        [Required]
        public float DeviationChance { get; set; }

        /// <summary>
        /// Максимальный % отклонения
        /// </summary>
        [Column("MaxDeviationPercent")]
        [Range(0.5, 0.9)]
        [Required]
        public float MaxDeviationPercent { get; set; }

        /// <summary>
        /// Мин-раз игр для буста
        /// </summary>
        [Column("MinGamesForBoost")]
        [Range(0, int.MaxValue)]
        [Required]
        public int MinGamesForBoost { get; set; }

        /// <summary>
        /// Шанс буста
        /// </summary>
        [Column("WinBoostChance")]
        [Range(0, 1)]
        [Required]
        public float WinBoostChance { get; set; }

        /// <summary>
        /// Множитель при бусте
        /// </summary>
        [Column("WinBoostMultiplier")]
        [Range(1, float.MaxValue)]
        [Required]
        public float WinBoostMultiplier { get; set; }

        /// <summary>
        /// Шанс хард-ресета
        /// </summary>
        [Column("HardResetChance")]
        [Range(0, 1)]
        [Required]
        public float HardResetChance { get; set; }

        /// <summary>
        /// Цель контроля
        /// </summary>
        [Column("ControlGoal")]
        [Range(0, float.MaxValue)]
        [Required]
        public float ControlGoal { get; set; }

        /// <summary>
        /// Скорость вращения барабана
        /// </summary>
        [Column("ReelSpeed")]
        [Required]
        public float ReelSpeed { get; set; }

        /// <summary>
        /// Порог для вертикальной линии
        /// </summary>
        [Column("UprightThresholdForLine")]
        [Required]
        public float UprightThresholdForLine { get; set; }

        /// <summary>
        /// Шанс (0-1) получения множителя больше 10x
        /// </summary>
        [Column("ProbabilityFor10")]
        [Range(0, 1)]
        public float? ProbabilityFor10 { get; set; }

        /// <summary>
        /// Шанс (0-1) получения множителя 5-10x
        /// </summary>
        [Column("ProbabilityFor5")]
        [Range(0, 1)]
        public float? ProbabilityFor5 { get; set; }

        /// <summary>
        /// Шанс (0-1) получения множителя 3-5x
        /// </summary>
        [Column("ProbabilityFor3")]
        [Range(0, 1)]
        public float? ProbabilityFor3 { get; set; }

        /// <summary>
        /// Шанс (0-1) получения множителя 2-3x
        /// </summary>
        [Column("ProbabilityFor2")]
        [Range(0, 1)]
        public float? ProbabilityFor2 { get; set; }

        /// <summary>
        /// Шанс (0-1) получения множителя 1.5-2x
        /// </summary>
        [Column("ProbabilityFor1_5")]
        [Range(0, 1)]
        public float? ProbabilityFor1_5 { get; set; }

        /// <summary>
        /// Шанс (0-1) получения множителя 1.2-1.5x
        /// </summary>
        [Column("BaseMultiplierProbability")]
        [Range(0, 1)]
        public float? BaseMultiplierProbability { get; set; }
    }
}
