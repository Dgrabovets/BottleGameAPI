using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Controller for notifications from the payment provider
    /// </summary>
    [Route("api/webhook")]
    [Tags("Webhooks")]
    public class WebhookController : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Notification handling service
        /// </summary>
        private readonly IPaymentWebhookService _webhookService;

        /// <summary>
        /// Largest accepted notification body in bytes. A YooKassa notification takes a few kilobytes
        /// </summary>
        private const long MAX_BODY_SIZE = 64 * 1024;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of <see cref="WebhookController"/>
        /// </summary>
        /// <param name="webhookService">Notification handling service</param>
        public WebhookController(IPaymentWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        #region Action Methods

        /// <summary>
        /// YooKassa notifications about payments and payouts.
        /// The address is set in the YooKassa dashboard: Integration, HTTP notifications
        /// </summary>
        /// <returns>Result of handling the notification</returns>
        [HttpPost("yookassa")]
        [RequestSizeLimit(MAX_BODY_SIZE)]
        [EnableRateLimiting("webhook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> YooKassaWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var request = new WebhookRequestInfo(
                body,
                Request.Method,
                Request.ContentType,
                // Behind a trusted proxy the forwarded headers middleware has already put the real client address here
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

            return ToActionResult(await _webhookService.HandleNotificationAsync(request));
        }

        #endregion Action Methods
    }
}
