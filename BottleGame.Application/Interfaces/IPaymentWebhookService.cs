using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;

namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Handling of notifications from the payment provider
    /// </summary>
    public interface IPaymentWebhookService
    {
        /// <summary>
        /// Handles an incoming notification about a payment or payout.
        /// The authenticity is checked by asking the provider for the object again; the body of the notification is not used
        /// </summary>
        /// <param name="request">Incoming webhook request</param>
        /// <returns>Result of the handling; a repeated delivery of an already handled notification counts as success</returns>
        Task<Result> HandleNotificationAsync(WebhookRequestInfo request);
    }
}
