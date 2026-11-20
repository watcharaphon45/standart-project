. "$PSScriptRoot/Environment.ps1"
dotnet restore clean.sln --disable-parallel --disable-build-servers
if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed.' }
npm.cmd ci --prefix src/Web/spa
if ($LASTEXITCODE -ne 0) { throw 'npm restore failed.' }
