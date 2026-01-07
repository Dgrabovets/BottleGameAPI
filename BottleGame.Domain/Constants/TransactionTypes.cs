namespace BottleGame.Domain.Constants
{
    /// <summary>
    /// Типы денежных транзакций
    /// </summary>
    public static class TransactionTypes
    {
        /// <summary>
        /// Пополнение баланса (депозит)
        /// </summary>
        public const int Deposit = 1;

        /// <summary>
        /// Вывод средств
        /// </summary>
        public const int Withdrawal = 2;

        /// <summary>
        /// Названия типов для логирования
        /// </summary>
        public static string GetName(int typeId) => typeId switch
        {
            Deposit => "Депозит",
            Withdrawal => "Вывод",
            _ => "Неизвестный тип"
        };
    }
}


