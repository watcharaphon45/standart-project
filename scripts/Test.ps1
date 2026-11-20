. "$PSScriptRoot/Test-Backend.ps1"
Push-Location src/Web/spa
try {
    npm.cmd run test:unit
    if ($LASTEXITCODE -ne 0) { throw 'Frontend tests failed.' }
    npm.cmd run build
    if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
} finally { Pop-Location }
