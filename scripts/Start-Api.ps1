. "$PSScriptRoot/Environment.ps1"
dotnet run --project src/Web --no-build --launch-profile https
exit $LASTEXITCODE
