// VegaBase.API/Middleware/ExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VegaBase.Core.Common;

namespace VegaBase.API.Middleware;

/// <summary>
/// Catches unhandled exceptions, returns a generic 500 JSON response with a trace ID,
/// and logs the full exception at Error level.
/// <para>
/// <b>PII redaction (F5):</b> set <see cref="SanitizeLogMessage"/> to strip sensitive data from
/// exception messages before they reach the log sink. The delegate receives the raw message and
/// must return a safe replacement. Example: replace email addresses with "[redacted]".
/// </para>
/// <para>
/// <b>Log-level guidance (F6):</b> configure log levels via <c>appsettings.json</c> /
/// environment variables using the standard <c>Logging:LogLevel</c> key hierarchy,
/// e.g. <c>Logging__LogLevel__VegaBase.API=Warning</c>.
/// </para>
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Optional delegate that sanitizes an exception message before it is logged.
    /// Set this at startup to redact PII (passwords, emails, tokens) from log output.
    /// Returns the original message when null.
    /// </summary>
    private static volatile Func<string, string>? _sanitizeLogMessage;
    public static Func<string, string>? SanitizeLogMessage
    {
        get => _sanitizeLogMessage;
        set => _sanitizeLogMessage = value;
    }

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                _logger.LogInformation("Request cancelled by client [TraceId={TraceId}]", context.TraceIdentifier);
                return;
            }

            var traceId      = context.TraceIdentifier;
            var rawMessage   = ex.Message;
            var safeMessage  = SanitizeLogMessage != null
                ? SanitizeLogMessage(rawMessage)
                : rawMessage;

            _logger.LogError(ex, "Unhandled exception: {Message} [TraceId={TraceId}]", safeMessage, traceId);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started — cannot write 500 [TraceId={TraceId}]", traceId);
                throw;
            }

            context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object>.Fail("Đã xảy ra lỗi không mong muốn.", traceId);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
