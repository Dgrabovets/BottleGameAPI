using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер уведомлений от платёжного провайдера.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="WebhookController"/>.
    /// </remarks>
    /// <param name="webhookService">Сервис обработки уведомлений.</param>
    [Route("api/webhook")]
    [Tags("Webhooks")]
    public class WebhookController(IPaymentWebhookService webhookService) : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Сервис обработки уведомлений.
        /// </summary>
        private readonly IPaymentWebhookService _webhookService = webhookService;

        #endregion Fields

        #region Action Methods

        /// <summary>
        /// Уведомления ЮKassa о платежах и выплатах.
        /// Адрес указывается в личном кабинете ЮKassa: Интеграция, HTTP-уведомления.
        /// </summary>
        /// <returns>Результат обработки уведомления.</returns>
        [HttpPost("yookassa")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> YooKassaWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var request = new WebhookRequestInfo(
                body,
                Request.Method,
                Request.ContentType,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString());

            return ToActionResult(await _webhookService.HandleNotificationAsync(request));
        }

        #endregion Action Methods

        #region Helper Methods

        /// <summary>
        /// Получает IP-адрес клиента для журнала.
        /// </summary>
        /// <returns>IP-адрес клиента или null.</returns>
        private string? GetClientIpAddress() =>
            Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? HttpContext.Connection.RemoteIpAddress?.ToString();

        #endregion Helper Methods
    }
}
