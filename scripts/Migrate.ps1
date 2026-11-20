. "$PSScriptRoot/Environment.ps1"
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'Tool restore failed.' }
dotnet build clean.sln --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
dotnet ef database update --project src/Infrastructure --startup-project src/Web --context OrganizationDbContext --no-build
if ($LASTEXITCODE -ne 0) { throw 'DB migration failed.' }
dotnet ef database update --project src/Infrastructure --startup-project src/Web --context SystemDbContext --no-build
if ($LASTEXITCODE -ne 0) { throw 'SU migration failed.' }
