. "$PSScriptRoot/Environment.ps1"
$env:CLEAN_TEST_DATABASE = $env:ConnectionStrings__Database
$env:CLEAN_RUN_BROWSER = "1"
try {
    npm.cmd run build --prefix src/Web/spa
    if ($LASTEXITCODE -ne 0) { throw "Frontend build failed." }
    dotnet test tests/clean.Tests/clean.Tests.csproj --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~BrowserRunsCompanyFlowAgainstRealApi --logger "console;verbosity=minimal"
    if ($LASTEXITCODE -ne 0) { throw "Browser test failed." }
} finally { Remove-Item Env:CLEAN_RUN_BROWSER -ErrorAction SilentlyContinue }
