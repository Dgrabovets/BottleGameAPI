namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Входящий webhook-запрос от платёжной системы: тело и метаданные HTTP
    /// </summary>
    /// <param name="Body">Сырое тело запроса</param>
    /// <param name="HttpMethod">HTTP-метод</param>
    /// <param name="ContentType">Заголовок Content-Type</param>
    /// <param name="IpAddress">IP-адрес отправителя</param>
    /// <param name="UserAgent">Заголовок User-Agent</param>
    public record WebhookRequestInfo(
        string Body,
        string HttpMethod,
        string? ContentType,
        string? IpAddress,
        string? UserAgent);
}
