namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Результат создания платежа: успех или ошибка
    /// </summary>
    public class CreatePaymentResult
    {
        public bool IsSuccess { get; set; }
        public CreatePaymentResponse? Payment { get; set; }
        public string? ErrorMessage { get; set; }
    }
}