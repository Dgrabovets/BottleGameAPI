using BottleGame.API.Controllers;
using BottleGame.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;

namespace BottleGame.UnitTests.Controllers;

/// <summary>
/// The anonymous webhook has its own body size limit and rate limit, and a body over the limit answers 413
/// </summary>
public class WebhookLimitsTests
{
    /// <summary>
    /// The webhook action carries a request size limit and the dedicated rate-limit policy
    /// </summary>
    [Fact]
    public void YooKassaWebhook_HasSizeLimitAndOwnRateLimitPolicy()
    {
        var action = typeof(WebhookController).GetMethod(nameof(WebhookController.YooKassaWebhook))!;

        action.GetCustomAttribute<RequestSizeLimitAttribute>().Should().NotBeNull();
        action.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName.Should().Be("webhook");
    }

    /// <summary>
    /// A body over the size limit answers with the status of the framework, not with 500
    /// </summary>
    [Fact]
    public async Task BodyTooLarge_Returns413()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(x => x.EnvironmentName).Returns("Production");
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge),
            NullLogger<GlobalExceptionMiddleware>.Instance,
            environment.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }
}
