using System.ComponentModel.DataAnnotations;

namespace BottleGameServer.Models.DTOs.Requests
{
    /// <summary>
    /// Модель запроса денежной транзакции
    /// </summary>
    public class MoneyTransactionRequest
    {
        /// <summary>
        /// Признак, пополнение ли
        /// </summary>
        [Required]
        public bool IsDeposit { get; set; }

        /// <summary>
        /// Сумма транзакции
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Сумма транзакции должна быть положительной")]
        public int Amount { get; set; }
    }
}
