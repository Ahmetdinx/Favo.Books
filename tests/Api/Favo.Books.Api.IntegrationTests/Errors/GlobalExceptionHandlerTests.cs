using System.Net;
using System.Text.Json;

using Favo.Books.Api.Infrastructure.Errors;

namespace Favo.Books.Api.IntegrationTests.Errors;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public void MapException_WhenJsonException_ShouldReturnBadGateway()
    {
        AssertMapping(
            new JsonException(),
            HttpStatusCode.BadGateway,
            "Dış servis geçersiz cevap döndürdü.");
    }

    [Fact]
    public void MapException_WhenHttpRequestException_ShouldReturnServiceUnavailable()
    {
        AssertMapping(
            new HttpRequestException(),
            HttpStatusCode.ServiceUnavailable,
            "Dış servise ulaşılamıyor.");
    }

    [Fact]
    public void MapException_WhenOperationIsCanceled_ShouldReturnGatewayTimeout()
    {
        AssertMapping(
            new OperationCanceledException(),
            HttpStatusCode.GatewayTimeout,
            "İstek zaman aşımına uğradı.");
    }

    [Fact]
    public void MapException_WhenExceptionIsUnknown_ShouldReturnInternalServerError()
    {
        AssertMapping(
            new InvalidOperationException(),
            HttpStatusCode.InternalServerError,
            "Beklenmeyen bir sunucu hatası oluştu.");
    }

    private static void AssertMapping(
        Exception exception,
        HttpStatusCode expectedStatusCode,
        string expectedTitle)
    {
        (
            int statusCode,
            string title,
            string detail) =
                GlobalExceptionHandler.MapException(
                    exception);

        Assert.Equal(
            (int)expectedStatusCode,
            statusCode);

        Assert.Equal(
            expectedTitle,
            title);

        Assert.False(
            string.IsNullOrWhiteSpace(detail));
    }
}