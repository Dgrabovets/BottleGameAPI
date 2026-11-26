namespace BottleGame.Domain.Constants
{
    /// <summary>
    /// Types of money transactions
    /// </summary>
    public static class TransactionTypes
    {
        /// <summary>
        /// Balance top-up (deposit)
        /// </summary>
        public const int DEPOSIT = 1;

        /// <summary>
        /// Withdrawal of funds
        /// </summary>
        public const int WITHDRAWAL = 2;

        /// <summary>
        /// Type names for logging
        /// </summary>
        public static string GetName(int typeId) => typeId switch
        {
            DEPOSIT => "Deposit",
            WITHDRAWAL => "Withdrawal",
            _ => "Unknown type"
        };
    }
}
