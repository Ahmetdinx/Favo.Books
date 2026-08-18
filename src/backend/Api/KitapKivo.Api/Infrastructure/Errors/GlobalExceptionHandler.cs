using System.Text.Json;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KitapKivo.Api.Infrastructure.Errors;

internal sealed class GlobalExceptionHandler
    : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler>
        _logger;

    private readonly IProblemDetailsService
        _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService =
            problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted
                .IsCancellationRequested)
        {
            return false;
        }

        (int statusCode, string title, string detail) =
            MapException(exception);

        _logger.LogError(
            exception,
            "Unhandled exception while processing " +
            "{Method} {Path}. TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode =
            statusCode;

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance =
                httpContext.Request.Path.Value
        };

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        await _problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problemDetails
            });

        return true;
    }

    internal static (
        int StatusCode,
        string Title,
        string Detail)
        MapException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            JsonException =>
            (
                StatusCodes.Status502BadGateway,
                "Dış servis geçersiz cevap döndürdü.",
                "Dış servisten alınan cevap işlenemedi."
            ),

            HttpRequestException =>
            (
                StatusCodes.Status503ServiceUnavailable,
                "Dış servise ulaşılamıyor.",
                "Dış servise yapılan istek başarısız oldu."
            ),

            OperationCanceledException =>
            (
                StatusCodes.Status504GatewayTimeout,
                "İstek zaman aşımına uğradı.",
                "İşlem belirlenen süre içinde tamamlanamadı."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "Beklenmeyen bir sunucu hatası oluştu.",
                "İstek işlenirken beklenmeyen bir hata oluştu."
            )
        };
    }
}