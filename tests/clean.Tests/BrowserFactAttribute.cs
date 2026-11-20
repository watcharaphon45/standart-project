namespace clean.Tests;
public sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("CLEAN_RUN_BROWSER") != "1")
            Skip = "Run scripts/Test-Browser.ps1 to run Chromium against the real HTTPS API and PostgreSQL.";
    }
}
