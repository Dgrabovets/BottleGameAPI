using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Обработка уведомлений платёжного провайдера
    /// </summary>
    public interface IPaymentWebhookService
    {
        /// <summary>
        /// Обрабатывает входящее уведомление о платеже или выплате.
        /// Подлинность проверяется повторным запросом объекта у провайдера, содержимое тела уведомления не используется
        /// </summary>
        /// <param name="request">Входящий webhook-запрос</param>
        /// <returns>Результат обработки; повторная доставка уже обработанного уведомления считается успехом</returns>
        Task<Result> HandleNotificationAsync(WebhookRequestInfo request);
    }
}
