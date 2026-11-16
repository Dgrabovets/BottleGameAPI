namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Response with a player's balance
    /// </summary>
    public class GetPlayerBalanceResponse
    {
        /// <summary>
        /// Player id
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// Player balance
        /// </summary>
        public decimal Balance { get; set; }
    }
}
