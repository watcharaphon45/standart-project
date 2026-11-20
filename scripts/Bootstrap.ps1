param([Parameter(Mandatory=$true)][string]$UserName, [Parameter(Mandatory=$true)][string]$Email)
. "$PSScriptRoot/Environment.ps1"
$password = Read-Host 'Temporary password (15-128 characters; expires in 24 hours)' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
try {
    $env:Bootstrap__UserName = $UserName
    $env:Bootstrap__Email = $Email
    $env:Bootstrap__Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    dotnet run --project src/Web --no-build --no-launch-profile -- --bootstrap
    if ($LASTEXITCODE -ne 0) { throw 'Bootstrap failed.' }
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    Remove-Item Env:Bootstrap__Password -ErrorAction SilentlyContinue
}
