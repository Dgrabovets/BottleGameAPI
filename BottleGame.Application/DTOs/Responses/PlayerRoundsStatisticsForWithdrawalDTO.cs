namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Statistics of the rounds a player played for a withdrawal
    /// </summary>
    public class PlayerRoundsStatisticsForWithdrawalDTO
    {
        /// <summary>
        /// Player data
        /// </summary>
        public PlayerDTO Player { get; set; } = null!;

        /// <summary>
        /// Last deposit
        /// </summary>
        public TransactionDTO? LastDeposit { get; set; }

        /// <summary>
        /// Total number of rounds played
        /// </summary>
        public int TotalRoundsPlayed { get; set; }
    }
}
