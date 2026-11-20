using clean.Web.Configuration;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace clean.Tests;

public sealed class LocalDevelopmentDatabaseTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "clean-config-" + Guid.NewGuid().ToString("N"));

    public LocalDevelopmentDatabaseTests()
    {
        Directory.CreateDirectory(Path.Combine(root, ".local"));
        Directory.CreateDirectory(Path.Combine(root, "src", "Web"));
        File.WriteAllText(Path.Combine(root, "clean.sln"), "");
    }

    [Fact]
    public void IdeLaunchFindsWorkspacePasswordAndEscapesConnectionStringCharacters()
    {
        File.WriteAllText(Path.Combine(root, ".local", "database.env"), "POSTGRES_PASSWORD=test;Password=example=\n");
        var configuration = new ConfigurationManager();
        LocalDevelopmentDatabase.Apply(configuration, "Development", Path.Combine(root, "src", "Web"));
        var connection = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Database"));
        Assert.Equal("test;Password=example=", connection.Password);
        Assert.Equal(55432, connection.Port);
        Assert.Equal("clean_dev", connection.Database);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void NonDevelopmentNeverLoadsLocalSecrets(string environment)
    {
        var configuration = new ConfigurationManager();
        LocalDevelopmentDatabase.Apply(configuration, environment, root);
        Assert.Null(configuration.GetConnectionString("Database"));
    }

    [Fact]
    public void ExplicitConnectionStringTakesPriority()
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:Database"] = "Host=custom;Database=custom";
        LocalDevelopmentDatabase.Apply(configuration, "Development", root);
        Assert.Equal("Host=custom;Database=custom", configuration.GetConnectionString("Database"));
    }

    [Fact]
    public void MissingLocalFileExplainsSetupWithoutExposingSecrets()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            LocalDevelopmentDatabase.Apply(new ConfigurationManager(), "Development", root));
        Assert.Contains("Start-Database.ps1", error.Message);
    }

    public void Dispose() => Directory.Delete(root, recursive: true);
}
