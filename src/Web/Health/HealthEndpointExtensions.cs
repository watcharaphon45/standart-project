using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace clean.Web.Health;

public static class HealthEndpointExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddCleanHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"]);
        return services;
    }

    public static WebApplication MapCleanHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteLivenessResponseAsync
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = WriteReadinessResponseAsync
        }).AllowAnonymous();

        return app;
    }

    private static Task WriteLivenessResponseAsync(HttpContext context, HealthReport _)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.ContentType = "application/json; charset=utf-8";
        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            new { status = "ok" },
            JsonOptions,
            context.RequestAborted);
    }

    private static Task WriteReadinessResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.ContentType = "application/json; charset=utf-8";

        // Expose status labels only; never serialize exception text, connection strings, or descriptions.
        var checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.Status == HealthStatus.Healthy ? "ready" : "unavailable",
            StringComparer.Ordinal);

        var payload = new
        {
            status = report.Status == HealthStatus.Healthy ? "ready" : "unavailable",
            checks
        };

        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            payload,
            JsonOptions,
            context.RequestAborted);
    }
}
