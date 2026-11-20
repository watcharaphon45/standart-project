param(
    [Parameter(Mandatory)]
    [string]$BackupPath,
    [string]$TargetDatabase,
    [switch]$Drill,
    [switch]$Force,
    [string]$ContainerName = 'clean-starter-dev-postgres',
    [ValidateSet('auto', 'native', 'podman', 'docker')]
    [string]$Engine = 'auto'
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/Environment.ps1"

$workspace = (Get-Location).Path
$resolvedBackup = if ([IO.Path]::IsPathRooted($BackupPath)) { $BackupPath } else { Join-Path $workspace $BackupPath }
if (-not (Test-Path -LiteralPath $resolvedBackup)) { throw "Backup file not found: $resolvedBackup" }

$checksumPath = "$resolvedBackup.sha256"
if (Test-Path -LiteralPath $checksumPath) {
    $expectedHash = ((Get-Content -LiteralPath $checksumPath -TotalCount 1) -split '\s+')[0].Trim().ToUpperInvariant()
    $actualHash = (Get-FileHash -LiteralPath $resolvedBackup -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($expectedHash -ne $actualHash) {
        throw "Backup checksum mismatch for $resolvedBackup."
    }
    Write-Host "Verified SHA256 checksum ($actualHash)."
}

if ($Drill -and -not [string]::IsNullOrWhiteSpace($TargetDatabase)) {
    throw 'Specify either -Drill or -TargetDatabase, not both.'
}
if (-not $Drill) {
    if ([string]::IsNullOrWhiteSpace($TargetDatabase) -or -not $Force) {
        throw 'Non-drill restore requires both -TargetDatabase and explicit -Force confirmation.'
    }
}

$csb = New-Object System.Data.Common.DbConnectionStringBuilder
$csb.ConnectionString = $env:ConnectionStrings__Database
$dbHost = if ($csb.ContainsKey('Host')) { [string]$csb['Host'] } else { 'localhost' }
$dbPort = if ($csb.ContainsKey('Port')) { [string]$csb['Port'] } else { '5432' }
$dbAdmin = if ($csb.ContainsKey('Database')) { [string]$csb['Database'] } else { 'clean_dev' }
$dbUser = if ($csb.ContainsKey('Username')) { [string]$csb['Username'] } elseif ($csb.ContainsKey('User ID')) { [string]$csb['User ID'] } else { 'clean_dev' }
$dbPass = if ($csb.ContainsKey('Password')) { [string]$csb['Password'] } else { '' }

$restoreDb = if ($Drill) { "clean_restore_drill_$(Get-Date -Format 'yyyyMMdd_HHmmss')" } else { $TargetDatabase }
if ($restoreDb -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw 'Target database name contains unsupported characters.' }
if ($dbUser -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw 'Username contains unsupported characters.' }

function Resolve-RestoreEngine {
    param([string]$Requested, [string]$TargetContainer)

    if ($Requested -ne 'auto') { return @{ Mode = $Requested; Container = $TargetContainer } }
    if ((Get-Command pg_restore -ErrorAction SilentlyContinue) -and (Get-Command psql -ErrorAction SilentlyContinue)) {
        return @{ Mode = 'native'; Container = '' }
    }

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

    throw 'No pg_restore/psql client or running PostgreSQL container found. Install PostgreSQL 18 client tools or run scripts/Start-Database.ps1 with Podman.'
}

$resolved = Resolve-RestoreEngine -Requested $Engine -TargetContainer $ContainerName
$mode = $resolved.Mode
$activeContainer = $resolved.Container
$verifySql = 'SELECT (SELECT count(*) FROM "__DbMigrationsHistory") AS db_mig, (SELECT count(*) FROM "__SuMigrationsHistory") AS su_mig, (SELECT count(*) FROM "DbCompany") AS companies, (SELECT count(*) FROM "SuUser") AS users, (SELECT count(*) FROM "SuAuditEvent") AS audits;'

try {
    if ($mode -eq 'native') {
        $env:PGPASSWORD = $dbPass
        if ($Drill) {
            & psql --host $dbHost --port $dbPort --username $dbUser --dbname $dbAdmin --set ON_ERROR_STOP=1 --command "CREATE DATABASE `"$restoreDb`";"
            if ($LASTEXITCODE -ne 0) { throw 'Failed to create drill database.' }
        }
        try {
            & pg_restore --host $dbHost --port $dbPort --username $dbUser --dbname $restoreDb --no-owner --no-privileges --exit-on-error $resolvedBackup
            if ($LASTEXITCODE -ne 0) { throw 'pg_restore failed.' }
            $summary = & psql --host $dbHost --port $dbPort --username $dbUser --dbname $restoreDb --tuples-only --no-align --set ON_ERROR_STOP=1 --command $verifySql
            if ($LASTEXITCODE -ne 0) { throw 'Restore verification query failed.' }
            Write-Host ("Restore verified on {0}: {1}" -f $restoreDb, ($summary -join ' '))
        } finally {
            if ($Drill) {
                & psql --host $dbHost --port $dbPort --username $dbUser --dbname $dbAdmin --command "DROP DATABASE IF EXISTS `"$restoreDb`" WITH (FORCE);" 2>$null | Out-Null
                Write-Host "Dropped temporary drill database $restoreDb."
            }
        }
    } else {
        $containerTmp = "/tmp/clean_restore_$([Guid]::NewGuid().ToString('N')).dump"
        try {
            & $mode cp $resolvedBackup "${activeContainer}:${containerTmp}"
            if ($LASTEXITCODE -ne 0) { throw "$mode cp failed." }

            if ($Drill) {
                $psqlScript = 'PGPASSWORD=${POSTGRES_PASSWORD} exec psql --username=$1 --dbname=$2 --set=ON_ERROR_STOP=1'
                "CREATE DATABASE `"$restoreDb`";" | & $mode exec -i $activeContainer sh -c $psqlScript _ $dbUser $dbAdmin
                if ($LASTEXITCODE -ne 0) { throw 'Failed to create drill database inside container.' }
            }

            $restoreScript = 'PGPASSWORD=${POSTGRES_PASSWORD} exec pg_restore --username=$1 --dbname=$2 --no-owner --no-privileges --exit-on-error $3'
            & $mode exec $activeContainer sh -c $restoreScript _ $dbUser $restoreDb $containerTmp
            if ($LASTEXITCODE -ne 0) { throw "$mode pg_restore failed." }

            $verifyScript = 'PGPASSWORD=${POSTGRES_PASSWORD} exec psql --username=$1 --dbname=$2 --tuples-only --no-align --set=ON_ERROR_STOP=1'
            $summary = $verifySql | & $mode exec -i $activeContainer sh -c $verifyScript _ $dbUser $restoreDb
            if ($LASTEXITCODE -ne 0) { throw 'Restore verification query failed.' }
            Write-Host ("Restore verified on {0} ({1}): {2}" -f $restoreDb, $mode, ($summary -join ' '))
        } finally {
            & $mode exec $activeContainer rm -f $containerTmp 2>$null | Out-Null
            if ($Drill) {
                $dropScript = 'PGPASSWORD=${POSTGRES_PASSWORD} exec psql --username=$1 --dbname=$2'
                "DROP DATABASE IF EXISTS `"$restoreDb`" WITH (FORCE);" | & $mode exec -i $activeContainer sh -c $dropScript _ $dbUser $dbAdmin 2>$null | Out-Null
                Write-Host "Dropped temporary drill database $restoreDb."
            }
        }
    }
} finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
