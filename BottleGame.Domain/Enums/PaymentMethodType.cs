namespace BottleGame.Domain.Enums
{
    /// <summary>
    /// Types of top-up methods (payment gateways)
    /// </summary>
    public enum PaymentMethodType
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
