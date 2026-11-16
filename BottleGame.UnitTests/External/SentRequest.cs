namespace BottleGame.UnitTests.External;

/// <summary>
/// A request captured by the recording HTTP handler
/// </summary>
/// <param name="Method">Method</param>
/// <param name="Uri">Uri</param>
/// <param name="Authorization">Authorization</param>
/// <param name="IdempotenceKey">Idempotence key</param>
/// <param name="Body">Body</param>
internal sealed record SentRequest(HttpMethod Method, string Uri, string? Authorization, string? IdempotenceKey, string? Body);
