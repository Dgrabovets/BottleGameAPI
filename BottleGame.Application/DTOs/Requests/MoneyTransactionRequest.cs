namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Запрос на создание денежной транзакции
    /// </summary>
    public class MoneyTransactionRequest
    {
        /// <summary>
        /// Сумма транзакции
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Является ли транзакция депозитом (true - депозит, false - вывод)
        /// </summary>
        public bool IsDeposit { get; set; }
    }
}
