param()
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not (Get-Command podman -ErrorAction SilentlyContinue)) {
    throw 'Install free Podman and initialize/start its machine first, or use PostgreSQL 18 directly with ConnectionStrings__Database. See docs/cost-policy.md. Existing database data is not changed.'
}
podman info --format '{{.Host.Os}}' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Start the Podman machine before starting PostgreSQL.' }
New-Item -ItemType Directory -Force -Path '.local' | Out-Null
$envPath = Join-Path (Get-Location) '.local/database.env'
if (-not (Test-Path -LiteralPath $envPath)) {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    $password = [Convert]::ToBase64String($bytes)
    [IO.File]::WriteAllText($envPath, "POSTGRES_PASSWORD=$password" + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
}
$container = 'clean-starter-dev-postgres'
podman container exists $container
$exists = $LASTEXITCODE
if ($exists -gt 1) { throw 'Could not inspect Podman containers.' }
if ($exists -eq 0) {
    podman start $container | Out-Null
} else {
    if (Get-NetTCPConnection -LocalPort 55432 -State Listen -ErrorAction SilentlyContinue) {
        throw 'Port 55432 is already in use. Back up and migrate the existing database before switching to Podman. No existing container or data has been removed.'
    }
    podman run --detach --name $container --env-file $envPath --env POSTGRES_DB=clean_dev --env POSTGRES_USER=clean_dev --publish 127.0.0.1:55432:5432 --volume clean-starter-dev-postgres-data:/var/lib/postgresql docker.io/library/postgres:18 | Out-Null
}
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL did not start.' }
$ready = $false
for ($attempt = 0; $attempt -lt 20; $attempt++) {
    podman exec $container pg_isready -U clean_dev -d clean_dev | Out-Null
    if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    Start-Sleep -Seconds 1
}
if (-not $ready) { throw 'PostgreSQL is not ready. Inspect the Podman container logs.' }
Write-Host 'PostgreSQL is available on localhost:55432. Credentials are in ignored .local/database.env.'
