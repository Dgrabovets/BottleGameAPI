namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Request to edit a player
    /// </summary>
    public class EditPlayerRequest
    {
        /// <summary>
        /// Player name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Avatar URL of the player
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Personal win chance (admin only)
        /// </summary>
        public float? WinChance { get; set; }

        /// <summary>
        /// Currency
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Payment method
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Type of payment details (for example, "card", "phone", "sbp").
        /// Must match one of the types available for the selected PaymentMethod
        /// </summary>
        public string? PaymentDetailType { get; set; }

        /// <summary>
        /// Payout method
        /// </summary>
        public string? PayoutMethod { get; set; }

        /// <summary>
        /// Wallet for payouts
        /// </summary>
        public string? Wallet { get; set; }

        /// <summary>
        /// Balance (admin only)
        /// </summary>
        public decimal? Balance { get; set; }
    }
}
