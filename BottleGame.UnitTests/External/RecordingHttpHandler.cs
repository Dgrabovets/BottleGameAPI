using FluentAssertions;
using System.Net;
using System.Text;

namespace BottleGame.UnitTests.External;

/// <summary>
/// HTTP handler that records every request and answers with a prepared response
/// </summary>
internal sealed class RecordingHttpHandler : HttpMessageHandler
{
    /// <summary>
    /// Status
    /// </summary>
    private HttpStatusCode _status = HttpStatusCode.OK;

    /// <summary>
    /// Body
    /// </summary>
    private string _body = "{}";

    /// <summary>
    /// Requests
    /// </summary>
    public List<SentRequest> Requests { get; } = [];

    /// <summary>
    /// Sets the status code and body for the next responses
    /// </summary>
    /// <param name="status">Status</param>
    /// <param name="body">Body</param>
    public void Respond(HttpStatusCode status, string body)
    {
        _status = status;
        _body = body;
    }

    /// <summary>
    /// Returns the only recorded request and fails when there is not exactly one
    /// </summary>
    /// <returns>The created value</returns>
    public SentRequest Single() => Requests.Should().ContainSingle().Subject;

    /// <summary>
    /// Records the request and answers with the configured response
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created value</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new SentRequest(
            request.Method,
            request.RequestUri!.ToString(),
            request.Headers.Authorization?.ToString(),
            request.Headers.TryGetValues("Idempotence-Key", out var keys) ? keys.Single() : null,
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));

        return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
    }
}
