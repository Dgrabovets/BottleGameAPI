using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BottleGame.Domain.Entities
{
    /// <summary>
    /// Game parameters
    /// </summary>
    [Table("GameParams")]
    public class GameParams
    {
        /// <summary>
        /// Parameters identifier
        /// </summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Base win chance
        /// </summary>
        [Column("BaseWinChance")]
        [Range(0, 1)]
        [Required]
        public float BaseWinChance { get; set; }

        /// <summary>
        /// Minimum deposit amount
        /// </summary>
        [Column("MinDepositAmount")]
        [Range(0, int.MaxValue)]
        [Required]
        public int MinDepositAmount { get; set; }

        /// <summary>
        /// Minimum number of rounds required to withdraw funds
        /// </summary>
        [Column("MinRoundsCountForWithdrawal")]
        [Range(0, int.MaxValue)]
        [Required]
        public int MinRoundsCountForWithdrawal { get; set; }

        /// <summary>
        /// Minimum win chance
        /// </summary>
        [Column("MinWinChance")]
        [Range(0, 1)]
        [Required]
        public float MinWinChance { get; set; }

        /// <summary>
        /// Maximum win chance
        /// </summary>
        [Column("MaxWinChance")]
        [Range(0, 1)]
        [Required]
        public float MaxWinChance { get; set; }

        /// <summary>
        /// Minimum chance decrease per throw
        /// </summary>
        [Column("ChanceDecPerThrowMin")]
        [Range(0.01, 1)]
        [Required]
        public float ChanceDecPerThrowMin { get; set; }

        /// <summary>
        /// Maximum chance decrease per throw
        /// </summary>
        [Column("ChanceDecPerThrowMax")]
        [Range(0.01, 1)]
        [Required]
        public float ChanceDecPerThrowMax { get; set; }

        /// <summary>
        /// Chance decrease per spin
        /// </summary>
        [Column("ChanceDecPerRotation")]
        [Range(0.01, 0.2)]
        [Required]
        public float ChanceDecPerRotation { get; set; }

        /// <summary>
        /// Minimum chance after a spin
        /// </summary>
        [Column("MinRotationWinChance")]
        [Range(0, 0.5)]
        [Required]
        public float MinRotationWinChance { get; set; }

        /// <summary>
        /// Minimum multiplier
        /// </summary>
        [Column("MinMultiplier")]
        [Range(1, float.MaxValue)]
        [Required]
        public float MinMultiplier { get; set; }

        /// <summary>
        /// Maximum multiplier
        /// </summary>
        [Column("MaxMultiplier")]
        [Range(1, float.MaxValue)]
        [Required]
        public float MaxMultiplier { get; set; }

        /// <summary>
        /// Initial balance loss
        /// </summary>
        [Column("InitialBalanceLoss")]
        [Range(0.1, 0.15)]
        [Required]
        public float InitialBalanceLoss { get; set; }

        /// <summary>
        /// Deviation chance
        /// </summary>
        [Column("DeviationChance")]
        [Range(0, 1)]
        [Required]
        public float DeviationChance { get; set; }

        /// <summary>
        /// Maximum deviation %
        /// </summary>
        [Column("MaxDeviationPercent")]
        [Range(0.5, 0.9)]
        [Required]
        public float MaxDeviationPercent { get; set; }

        /// <summary>
        /// Minimum games for a boost
        /// </summary>
        [Column("MinGamesForBoost")]
        [Range(0, int.MaxValue)]
        [Required]
        public int MinGamesForBoost { get; set; }

        /// <summary>
        /// Boost chance
        /// </summary>
        [Column("WinBoostChance")]
        [Range(0, 1)]
        [Required]
        public float WinBoostChance { get; set; }

        /// <summary>
        /// Multiplier on a boost
        /// </summary>
        [Column("WinBoostMultiplier")]
        [Range(1, float.MaxValue)]
        [Required]
        public float WinBoostMultiplier { get; set; }

        /// <summary>
        /// Hard reset chance
        /// </summary>
        [Column("HardResetChance")]
        [Range(0, 1)]
        [Required]
        public float HardResetChance { get; set; }

        /// <summary>
        /// Control target
        /// </summary>
        [Column("ControlGoal")]
        [Range(0, float.MaxValue)]
        [Required]
        public float ControlGoal { get; set; }

        /// <summary>
        /// Drum spin speed
        /// </summary>
        [Column("ReelSpeed")]
        [Required]
        public float ReelSpeed { get; set; }

        /// <summary>
        /// Threshold for the vertical line
        /// </summary>
        [Column("UprightThresholdForLine")]
        [Required]
        public float UprightThresholdForLine { get; set; }

        /// <summary>
        /// Chance (0-1) of getting a multiplier above 10x
        /// </summary>
        [Column("ProbabilityFor10")]
        [Range(0, 1)]
        public float? ProbabilityFor10 { get; set; }

        /// <summary>
        /// Chance (0-1) of getting a multiplier of 5-10x
        /// </summary>
        [Column("ProbabilityFor5")]
        [Range(0, 1)]
        public float? ProbabilityFor5 { get; set; }

        /// <summary>
        /// Chance (0-1) of getting a multiplier of 3-5x
        /// </summary>
        [Column("ProbabilityFor3")]
        [Range(0, 1)]
        public float? ProbabilityFor3 { get; set; }

        /// <summary>
        /// Chance (0-1) of getting a multiplier of 2-3x
        /// </summary>
        [Column("ProbabilityFor2")]
        [Range(0, 1)]
        public float? ProbabilityFor2 { get; set; }

        /// <summary>
        /// Chance (0-1) of getting a multiplier of 1.5-2x
        /// </summary>
        [Column("ProbabilityFor1_5")]
        [Range(0, 1)]
        public float? ProbabilityFor1_5 { get; set; }

        /// <summary>
        /// Chance (0-1) of getting a multiplier of 1.2-1.5x
        /// </summary>
        [Column("BaseMultiplierProbability")]
        [Range(0, 1)]
        public float? BaseMultiplierProbability { get; set; }
    }
}
