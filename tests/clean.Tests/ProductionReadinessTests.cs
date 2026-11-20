using clean.Web.Configuration;
using clean.Web.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace clean.Tests;

public sealed class ProductionReadinessTests
{
    [Fact]
    public void ParseCookieSameSiteAcceptsValidValuesAndRejectsUnsupportedValues()
    {
        Assert.Equal(SameSiteMode.None, ProductionReadiness.ParseCookieSameSite(BuildConfig(null)));
        Assert.Equal(SameSiteMode.None, ProductionReadiness.ParseCookieSameSite(BuildConfig("none")));
        Assert.Equal(SameSiteMode.Lax, ProductionReadiness.ParseCookieSameSite(BuildConfig("Lax")));
        Assert.Equal(SameSiteMode.Strict, ProductionReadiness.ParseCookieSameSite(BuildConfig("STRICT")));
        Assert.Throws<InvalidOperationException>(() =>
            ProductionReadiness.ParseCookieSameSite(BuildConfig("Unspecified")));
        Assert.Throws<InvalidOperationException>(() =>
            ProductionReadiness.ParseCookieSameSite(BuildConfig("Invalid")));

        static IConfiguration BuildConfig(string? sameSite) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(sameSite is null
                    ? []
                    : [new KeyValuePair<string, string?>("Cookies:SameSite", sameSite)])
                .Build();
    }

    [Fact]
    public void ValidateCorsOriginsEnforcesExplicitHttpsInProduction()
    {
        var empty = new ConfigurationBuilder().Build();
        Assert.Throws<InvalidOperationException>(() => ProductionReadiness.ValidateCorsOrigins(empty, false));

        var wildcard = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("Cors:AllowedOrigins:0", "*")])
            .Build();
        Assert.Throws<InvalidOperationException>(() => ProductionReadiness.ValidateCorsOrigins(wildcard, true));

        var httpOrigin = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("Cors:AllowedOrigins:0", "http://app.example.com")])
            .Build();
        Assert.Throws<InvalidOperationException>(() => ProductionReadiness.ValidateCorsOrigins(httpOrigin, false));
        Assert.Equal(["http://app.example.com"], ProductionReadiness.ValidateCorsOrigins(httpOrigin, true));

        var validProd = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("Cors:AllowedOrigins:0", "https://app.example.com/")])
            .Build();
        Assert.Equal(["https://app.example.com"], ProductionReadiness.ValidateCorsOrigins(validProd, false));

        var withPath = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("Cors:AllowedOrigins:0", "https://app.example.com/sub")])
            .Build();
        Assert.Throws<InvalidOperationException>(() => ProductionReadiness.ValidateCorsOrigins(withPath, false));
    }

    [Fact]
    public async Task DatabaseReadinessHealthCheckRedactsFailureDetails()
    {
        const string sentinel = "SentinelSecret_99182!";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>(
                    "ConnectionStrings:Database",
                    $"Host=127.0.0.1;Port=1;Database=missing_db;Username=nobody;Password={sentinel};Timeout=1;Command Timeout=1")
            ])
            .Build();

        var check = new DatabaseReadinessHealthCheck(configuration, NullLogger<DatabaseReadinessHealthCheck>.Instance);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.DoesNotContain(sentinel, result.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", result.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("nobody", result.Description ?? string.Empty, StringComparison.Ordinal);
    }
}
