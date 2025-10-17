namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Модель ответа на создание выплаты (payout) на процессинговой площадке
    /// </summary>
    /// <param name="payout_id">ID выплаты в системе процессинга</param>
    /// <param name="order_id">ID выплаты в вашей системе</param>
    /// <param name="balance">Новый баланс после выплаты в центах/копейках</param>
    public record CreatePayoutResponse(
        string payout_id,
        string order_id,
        int balance
    );
}