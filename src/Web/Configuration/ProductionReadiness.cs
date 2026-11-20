using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace clean.Web.Configuration;

public sealed record ProductionSettings(string[] AllowedOrigins, SameSiteMode CookieSameSite, bool ReverseProxyEnabled);

public static class ProductionReadiness
{
    public static string[] ValidateCorsOrigins(IConfiguration configuration, bool isDevelopment)
    {
        var rawOrigins = (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .Select(origin => origin.Trim())
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .ToArray();

        if (rawOrigins.Length == 0)
            throw new InvalidOperationException("Configure explicit Cors:AllowedOrigins.");

        var normalized = new List<string>(rawOrigins.Length);
        foreach (var origin in rawOrigins)
        {
            if (origin.Contains('*', StringComparison.Ordinal))
                throw new InvalidOperationException("Cors:AllowedOrigins must not contain wildcards.");

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                throw new InvalidOperationException($"Invalid CORS origin '{origin}'.");

            if (!string.Equals(uri.AbsolutePath, "/", StringComparison.Ordinal)
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment))
                throw new InvalidOperationException($"Cors:AllowedOrigins entry '{origin}' must not contain path, query, or fragment.");

            var schemeAllowed = isDevelopment
                ? uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                  || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                : uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

            if (!schemeAllowed)
                throw new InvalidOperationException("Non-development Cors:AllowedOrigins must use https://.");

            normalized.Add(uri.GetLeftPart(UriPartial.Authority));
        }

        return normalized.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static SameSiteMode ParseCookieSameSite(IConfiguration configuration)
    {
        var value = configuration["Cookies:SameSite"];
        if (string.IsNullOrWhiteSpace(value)) return SameSiteMode.None;

        return value.Trim().ToUpperInvariant() switch
        {
            "NONE" => SameSiteMode.None,
            "LAX" => SameSiteMode.Lax,
            "STRICT" => SameSiteMode.Strict,
            _ => throw new InvalidOperationException("Cookies:SameSite must be None, Lax, or Strict.")
        };
    }

    public static ProductionSettings ConfigureProductionReadiness(this WebApplicationBuilder builder)
    {
        var origins = ValidateCorsOrigins(builder.Configuration, builder.Environment.IsDevelopment());
        var sameSite = ParseCookieSameSite(builder.Configuration);
        var reverseProxyEnabled = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");

        if (reverseProxyEnabled)
        {
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = Math.Clamp(builder.Configuration.GetValue<int?>("ReverseProxy:ForwardLimit") ?? 1, 1, 10);

                var knownProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
                foreach (var proxy in knownProxies.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    if (!IPAddress.TryParse(proxy.Trim(), out var ip))
                        throw new InvalidOperationException($"Invalid ReverseProxy:KnownProxies address '{proxy}'.");
                    options.KnownProxies.Add(ip);
                }

                var knownNetworks = builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [];
                foreach (var cidr in knownNetworks.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    if (!System.Net.IPNetwork.TryParse(cidr.Trim(), out var network))
                        throw new InvalidOperationException($"Invalid ReverseProxy:KnownNetworks CIDR '{cidr}'.");
                    options.KnownIPNetworks.Add(network);
                }
            });
        }

        var keyDirectory = builder.Configuration["DataProtection:KeyDirectory"];
        if (!string.IsNullOrWhiteSpace(keyDirectory))
        {
            var appName = string.IsNullOrWhiteSpace(builder.Configuration["DataProtection:ApplicationName"])
                ? "clean"
                : builder.Configuration["DataProtection:ApplicationName"]!.Trim();
            var info = Directory.CreateDirectory(keyDirectory.Trim());
            builder.Services.AddDataProtection()
                .SetApplicationName(appName)
                .PersistKeysToFileSystem(info);
        }

        return new ProductionSettings(origins, sameSite, reverseProxyEnabled);
    }
}
