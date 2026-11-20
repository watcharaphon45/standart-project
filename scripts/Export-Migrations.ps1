param(
    [string]$OutputDir = '.local/artifacts/migrations',
    [switch]$NoBuild,
    [switch]$VerifyOnTempDb
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/Environment.ps1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$workspace = (Get-Location).Path
$resolvedOutputDir = if ([IO.Path]::IsPathRooted($OutputDir)) {
    $OutputDir
} else {
    Join-Path $workspace $OutputDir
}
New-Item -ItemType Directory -Force -Path $resolvedOutputDir | Out-Null

dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'Tool restore failed.' }

if (-not $NoBuild) {
    dotnet build clean.sln --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}

$orgSql = Join-Path $resolvedOutputDir '01-organization.sql'
$suSql = Join-Path $resolvedOutputDir '02-system.sql'

dotnet ef migrations script --project src/Infrastructure --startup-project src/Web --context OrganizationDbContext --idempotent --no-build --output $orgSql
if ($LASTEXITCODE -ne 0) { throw 'OrganizationDbContext migration script generation failed.' }

dotnet ef migrations script --project src/Infrastructure --startup-project src/Web --context SystemDbContext --idempotent --no-build --output $suSql
if ($LASTEXITCODE -ne 0) { throw 'SystemDbContext migration script generation failed.' }

foreach ($file in @($orgSql, $suSql)) {
    if (-not (Test-Path -LiteralPath $file)) { throw "Generated script not found: $file" }
    $info = Get-Item -LiteralPath $file
    if ($info.Length -le 0) { throw "Generated script is empty: $file" }
    $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
    Write-Host ("Generated: {0} ({1} bytes, SHA256={2})" -f $info.FullName, $info.Length, $hash)
}

if ($VerifyOnTempDb) {
    $env:CLEAN_TEST_DATABASE = $env:ConnectionStrings__Database
    dotnet test tests/clean.Tests/clean.Tests.csproj --no-build --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~MigrationSqlDryRunIsCleanAndIdempotent --logger "console;verbosity=minimal"
    if ($LASTEXITCODE -ne 0) { throw 'Temporary DB migration dry-run verification failed.' }
}
