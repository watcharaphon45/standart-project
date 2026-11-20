$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.local/dotnet-home'
$env:NUGET_PACKAGES = Join-Path (Get-Location) '.local/packages'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
if (-not $env:ConnectionStrings__Database) {
    $envPath = Join-Path (Get-Location) '.local/database.env'
    if (-not (Test-Path -LiteralPath $envPath)) { throw 'Run scripts/Start-Database.ps1 or set ConnectionStrings__Database.' }
    $entry = Get-Content -LiteralPath $envPath | Where-Object { $_.StartsWith('POSTGRES_PASSWORD=') } | Select-Object -First 1
    $password = $entry.Substring('POSTGRES_PASSWORD='.Length)
    $env:ConnectionStrings__Database = "Host=localhost;Port=55432;Database=clean_dev;Username=clean_dev;Password=$password"
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'
