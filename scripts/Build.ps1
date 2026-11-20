. "$PSScriptRoot/Environment.ps1"
dotnet publish src/Web/Web.csproj --configuration Release --no-restore --disable-build-servers -p:UseSharedCompilation=false --output .local/artifacts/api
if ($LASTEXITCODE -ne 0) { throw 'API publish failed.' }
npm.cmd run build --prefix src/Web/spa
if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
Write-Host 'API: .local/artifacts/api'
Write-Host 'Frontend: src/Web/spa/dist/clean/browser'
