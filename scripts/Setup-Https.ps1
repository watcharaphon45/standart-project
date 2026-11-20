$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$certificateDirectory = Join-Path $workspace '.local/https'
New-Item -ItemType Directory -Path $certificateDirectory -Force | Out-Null

dotnet dev-certs https --check --trust --quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Trust the ASP.NET Core localhost development certificate if Windows asks for confirmation.'
    dotnet dev-certs https --trust
    if ($LASTEXITCODE -ne 0) { throw 'Could not trust the localhost development certificate.' }
}

$certificatePath = Join-Path $certificateDirectory 'localhost.pem'
dotnet dev-certs https --export-path $certificatePath --format Pem --no-password
if ($LASTEXITCODE -ne 0) { throw 'Could not export the localhost certificate for Angular.' }
if (-not (Test-Path -LiteralPath (Join-Path $certificateDirectory 'localhost.key'))) {
    throw 'The development certificate private key was not exported.'
}
Write-Host 'Angular and ASP.NET Core can use the trusted localhost development certificate.'
