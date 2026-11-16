namespace BottleGame.Application.DTOs.Responses
{
    /// <summary>
    /// Player settings DTO
    /// </summary>
    public class PlayerOptionsDTO
    {
        /// <summary>
        /// Personal win chance
        /// </summary>
        public float WinChance { get; set; }

        /// <summary>
        /// Wallet for payouts
        /// </summary>
        public string? Wallet { get; set; }

        /// <summary>
        /// Currency (string)
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Payout method (string)
        /// </summary>
        public string PayoutMethod { get; set; } = string.Empty;

        /// <summary>
        /// Payment method (string)
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Type of payment details (for example, "card", "phone", "sbp")
        /// </summary>
        public string PaymentDetailType { get; set; } = string.Empty;
    }
}
