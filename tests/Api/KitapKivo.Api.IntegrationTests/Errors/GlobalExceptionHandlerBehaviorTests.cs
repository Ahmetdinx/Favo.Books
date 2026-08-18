using System.Text.Json;

using KitapKivo.Api.Infrastructure.Errors;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace KitapKivo.Api.IntegrationTests.Errors;

public sealed class GlobalExceptionHandlerBehaviorTests
    : IClassFixture<KitapKivoWebApplicationFactory>
{
    private readonly KitapKivoWebApplicationFactory
        _factory;

    public GlobalExceptionHandlerBehaviorTests(
        KitapKivoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsUnhandled_ShouldWriteProblemDetails()
    {
        // Arrange
        GlobalExceptionHandler handler =
            GetHandler();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = _factory.Services
        };

        httpContext.Request.Method =
            HttpMethods.Get;

        httpContext.Request.Path =
            "/test/unhandled-error";

        httpContext.Response.Body =
            new MemoryStream();

        // Act
        bool handled =
            await handler.TryHandleAsync(
                httpContext,
                new InvalidOperationException(
                    "Sensitive exception detail."),
                CancellationToken.None);

        // Assert
        Assert.True(handled);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            httpContext.Response.StatusCode);

        Assert.StartsWith(
            "application/problem+json",
            httpContext.Response.ContentType,
            StringComparison.OrdinalIgnoreCase);

        httpContext.Response.Body.Position = 0;

        using JsonDocument responseBody =
            await JsonDocument.ParseAsync(
                httpContext.Response.Body);

        JsonElement root =
            responseBody.RootElement;

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            root.GetProperty("status").GetInt32());

        Assert.Equal(
            "Beklenmeyen bir sunucu hatası oluştu.",
            root.GetProperty("title").GetString());

        Assert.Equal(
            httpContext.TraceIdentifier,
            root.GetProperty("traceId").GetString());

        Assert.DoesNotContain(
            "Sensitive exception detail.",
            root.GetRawText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryHandleAsync_WhenRequestIsAborted_ShouldNotWriteResponse()
    {
        // Arrange
        GlobalExceptionHandler handler =
            GetHandler();

        using CancellationTokenSource cancellation =
            new();

        cancellation.Cancel();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = _factory.Services,
            RequestAborted = cancellation.Token
        };

        httpContext.Response.Body =
            new MemoryStream();

        // Act
        bool handled =
            await handler.TryHandleAsync(
                httpContext,
                new OperationCanceledException(),
                CancellationToken.None);

        // Assert
        Assert.False(handled);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    private GlobalExceptionHandler GetHandler()
    {
        return _factory.Services
            .GetServices<IExceptionHandler>()
            .OfType<GlobalExceptionHandler>()
            .Single();
    }
}