using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace clean.Web.Health;

public sealed partial class DatabaseReadinessHealthCheck(
    IConfiguration configuration,
    ILogger<DatabaseReadinessHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString))
            return HealthCheckResult.Unhealthy("Database unavailable");

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            builder.Timeout = builder.Timeout <= 0 ? 5 : Math.Min(builder.Timeout, 5);
            builder.CommandTimeout = builder.CommandTimeout <= 0 ? 5 : Math.Min(builder.CommandTimeout, 5);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(timeoutCts.Token);
            await using var command = new NpgsqlCommand("SELECT 1", connection)
            {
                CommandTimeout = 5
            };
            await command.ExecuteScalarAsync(timeoutCts.Token);
            return HealthCheckResult.Healthy("ready");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogReadinessFailure(logger, ex);
            return HealthCheckResult.Unhealthy("Database unavailable");
        }
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Database readiness check failed.")]
    private static partial void LogReadinessFailure(ILogger logger, Exception exception);
}
