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
        public const int Deposit = 1;

        /// <summary>
        /// Withdrawal of funds
        /// </summary>
        public const int Withdrawal = 2;

        /// <summary>
        /// Type names for logging
        /// </summary>
        public static string GetName(int typeId) => typeId switch
        {
            Deposit => "Deposit",
            Withdrawal => "Withdrawal",
            _ => "Unknown type"
        };
    }
}


