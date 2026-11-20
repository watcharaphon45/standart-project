param(
    [string]$OutputPath,
    [string]$ContainerName = 'clean-starter-dev-postgres',
    [ValidateSet('auto', 'native', 'podman', 'docker')]
    [string]$Engine = 'auto'
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/Environment.ps1"

$csb = New-Object System.Data.Common.DbConnectionStringBuilder
$csb.ConnectionString = $env:ConnectionStrings__Database
$dbHost = if ($csb.ContainsKey('Host')) { [string]$csb['Host'] } else { 'localhost' }
$dbPort = if ($csb.ContainsKey('Port')) { [string]$csb['Port'] } else { '5432' }
$dbName = if ($csb.ContainsKey('Database')) { [string]$csb['Database'] } else { 'clean_dev' }
$dbUser = if ($csb.ContainsKey('Username')) { [string]$csb['Username'] } elseif ($csb.ContainsKey('User ID')) { [string]$csb['User ID'] } else { 'clean_dev' }
$dbPass = if ($csb.ContainsKey('Password')) { [string]$csb['Password'] } else { '' }

if ($dbName -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw 'Database name contains unsupported characters.' }
if ($dbUser -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw 'Username contains unsupported characters.' }

$workspace = (Get-Location).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $OutputPath = Join-Path $workspace ".local/backups/${dbName}_${stamp}.dump"
} elseif (-not [IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $workspace $OutputPath
}

$backupDir = Split-Path -Path $OutputPath -Parent
if (-not [string]::IsNullOrWhiteSpace($backupDir)) {
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
}

function Resolve-BackupEngine {
    param([string]$Requested, [string]$TargetContainer)

    if ($Requested -ne 'auto') { return @{ Mode = $Requested; Container = $TargetContainer } }
    if (Get-Command pg_dump -ErrorAction SilentlyContinue) { return @{ Mode = 'native'; Container = '' } }

    if (Get-Command podman -ErrorAction SilentlyContinue) {
        podman container exists $TargetContainer 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { return @{ Mode = 'podman'; Container = $TargetContainer } }
    }

    if (Get-Command docker -ErrorAction SilentlyContinue) {
        $running = docker ps --format '{{.Names}}' 2>$null
        if ($LASTEXITCODE -eq 0 -and $running) {
            $match = $running | Where-Object { $_ -eq $TargetContainer -or $_ -like '*postgres*' } | Select-Object -First 1
            if ($match) {
                Write-Warning "Using legacy Docker container '$match'. See docs/cost-policy.md before installing or migrating new environments."
                return @{ Mode = 'docker'; Container = $match }
            }
        }
    }

    throw 'No pg_dump client or running PostgreSQL container found. Install PostgreSQL 18 client tools or run scripts/Start-Database.ps1 with Podman.'
}

$resolved = Resolve-BackupEngine -Requested $Engine -TargetContainer $ContainerName
$mode = $resolved.Mode
$activeContainer = $resolved.Container

try {
    if ($mode -eq 'native') {
        $env:PGPASSWORD = $dbPass
        & pg_dump --host $dbHost --port $dbPort --username $dbUser --dbname $dbName --format=custom --no-owner --no-privileges --file $OutputPath
        if ($LASTEXITCODE -ne 0) { throw 'pg_dump failed.' }
    } else {
        $containerTmp = "/tmp/clean_backup_$([Guid]::NewGuid().ToString('N')).dump"
        try {
            $script = 'PGPASSWORD=${POSTGRES_PASSWORD} exec pg_dump --username=$1 --dbname=$2 --format=custom --no-owner --no-privileges --file=$3'
            & $mode exec $activeContainer sh -c $script _ $dbUser $dbName $containerTmp
            if ($LASTEXITCODE -ne 0) { throw "$mode pg_dump failed." }
            & $mode cp "${activeContainer}:${containerTmp}" $OutputPath
            if ($LASTEXITCODE -ne 0) { throw "$mode cp failed." }
        } finally {
            & $mode exec $activeContainer rm -f $containerTmp 2>$null | Out-Null
        }
    }
} finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}

if (-not (Test-Path -LiteralPath $OutputPath)) { throw "Backup file was not created: $OutputPath" }
$info = Get-Item -LiteralPath $OutputPath
if ($info.Length -le 0) { throw "Backup file is empty: $OutputPath" }

$hash = (Get-FileHash -LiteralPath $OutputPath -Algorithm SHA256).Hash
$checksumFile = "$OutputPath.sha256"
[IO.File]::WriteAllText($checksumFile, "$hash  $([IO.Path]::GetFileName($OutputPath))" + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))

Write-Host ("Backup created ({0}): {1} ({2} bytes, SHA256={3})" -f $mode, $info.FullName, $info.Length, $hash)
Write-Host "Checksum written: $checksumFile"
