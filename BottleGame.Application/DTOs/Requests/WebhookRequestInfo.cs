namespace BottleGame.Application.DTOs.Requests
{
    /// <summary>
    /// Incoming webhook request from the payment system: body and HTTP metadata
    /// </summary>
    /// <param name="Body">Raw request body</param>
    /// <param name="HttpMethod">HTTP method</param>
    /// <param name="ContentType">Content-Type header</param>
    /// <param name="IpAddress">IP address of the sender</param>
    /// <param name="UserAgent">User-Agent header</param>
    public record WebhookRequestInfo(
        string Body,
        string HttpMethod,
        string? ContentType,
        string? IpAddress,
        string? UserAgent);
}
