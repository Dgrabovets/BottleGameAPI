namespace BottleGame.Domain.Enums
{
    /// <summary>
    /// Types of payout methods (withdrawal gateways)
    /// </summary>
    public enum PayoutMethodType
    {
        /// <summary>
        /// Faster Payments System, rubles
        /// </summary>
        SbpRub,
        /// <summary>
        /// Sberbank transfer
        /// </summary>
        Sberbank,
        /// <summary>
        /// Bank card, rubles
        /// </summary>
        CardRub,
        /// <summary>
        /// Bank card, tenge
        /// </summary>
        CardKzt
    }
}
