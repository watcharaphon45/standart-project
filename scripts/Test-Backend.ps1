. "$PSScriptRoot/Environment.ps1"
$env:CLEAN_TEST_DATABASE = $env:ConnectionStrings__Database
dotnet test clean.sln --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) { throw 'Backend tests failed.' }
