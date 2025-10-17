using BottleGameServer.Processing.Models;

/// <summary>
/// Результат создания вывода: успех или ошибка
/// </summary>
public class CreatePayoutResult
{
    public bool IsSuccess { get; set; }
    public CreatePayoutResponse? Payout { get; set; }
    public string? ErrorMessage { get; set; }
}