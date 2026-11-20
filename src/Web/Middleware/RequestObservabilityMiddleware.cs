using System.Diagnostics;
using System.Security.Claims;

namespace clean.Web.Middleware;

public sealed partial class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger)
{
    public static string ResolveTraceId(HttpContext? context)
    {
        var raw = Activity.Current?.Id ?? context?.TraceIdentifier ?? "bootstrap";
        return raw.Length > 100 ? raw[..100] : raw;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = ResolveTraceId(context);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Trace-Id"] = traceId;
            return Task.CompletedTask;
        });

        using var scope = logger.BeginScope(new Dictionary<string, object?> { ["TraceId"] = traceId });
        var started = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            var elapsedMs = Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 2);
            var statusCode = context.Response.StatusCode;
            long? userId = long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
                ? parsed
                : null;
            var path = context.Request.Path.Value ?? "/";

            var level = statusCode >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : statusCode >= StatusCodes.Status400BadRequest
                    ? LogLevel.Warning
                    : context.Request.Path.StartsWithSegments("/health", StringComparison.Ordinal)
                        ? LogLevel.Debug
                        : LogLevel.Information;

            LogHttpRequest(logger, level, context.Request.Method, path, statusCode, elapsedMs, userId, traceId);
        }
    }

    [LoggerMessage(
        EventId = 1002,
        Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms (UserId={UserId}, TraceId={TraceId})")]
    private static partial void LogHttpRequest(
        ILogger logger,
        LogLevel level,
        string method,
        string path,
        int statusCode,
        double elapsedMs,
        long? userId,
        string traceId);
}
