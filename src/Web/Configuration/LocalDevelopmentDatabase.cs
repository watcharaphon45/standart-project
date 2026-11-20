using Npgsql;

namespace clean.Web.Configuration;

public static class LocalDevelopmentDatabase
{
    public static void AddLocalDevelopmentDatabase(this WebApplicationBuilder builder)
    {
        Apply(builder.Configuration, builder.Environment.EnvironmentName, builder.Environment.ContentRootPath);
    }

    public static void Apply(ConfigurationManager configuration, string environmentName, string contentRoot)
    {
        if (environmentName != Environments.Development
            || !string.IsNullOrWhiteSpace(configuration.GetConnectionString("Database"))) return;

        DirectoryInfo? directory = new(contentRoot);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "clean.sln")))
            directory = directory.Parent;

        var path = directory is null ? null : Path.Combine(directory.FullName, ".local", "database.env");
        if (path is null || !File.Exists(path))
            throw new InvalidOperationException(
                "Local database configuration is missing. Run scripts/Start-Database.ps1 from the workspace, "
                + "or configure ConnectionStrings__Database before starting the API.");

        const string prefix = "POSTGRES_PASSWORD=";
        var entries = File.ReadLines(path).Where(line => line.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        if (entries.Length != 1 || string.IsNullOrWhiteSpace(entries[0][prefix.Length..]))
            throw new InvalidOperationException(
                "Local database configuration is invalid. Set POSTGRES_PASSWORD in .local/database.env "
                + "or configure ConnectionStrings__Database.");

        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost", Port = 55432, Database = "clean_dev", Username = "clean_dev",
            Password = entries[0][prefix.Length..]
        };
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = connection.ConnectionString
        });
    }
}
