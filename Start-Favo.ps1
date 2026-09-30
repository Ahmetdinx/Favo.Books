$ErrorActionPreference = 'Stop'

# Resolve paths from this file so moving the repository does not break startup.
$taskRoot = $PSScriptRoot
$taskApi = Join-Path $taskRoot 'src/backend/Api/Favo.Books.Api/Favo.Books.Api.csproj'
$taskFrontend = Join-Path $taskRoot 'src/frontend'
if (-not (Test-Path -LiteralPath $taskApi)) { throw "API project not found: $taskApi" }

function Start-FavoTerminal([string]$Title, [string]$Directory, [string]$Command) {
    $taskDirectoryLiteral = $Directory.Replace("'", "''")
    $taskCommand = "`$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location -LiteralPath '$taskDirectoryLiteral'; $Command"
    $taskEncoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($taskCommand))
    Start-Process powershell.exe -WorkingDirectory $Directory -ArgumentList @('-NoExit', '-EncodedCommand', $taskEncoded)
}

$taskApiRunning = $false
try {
    $taskHealth = Invoke-RestMethod 'http://localhost:5117/' -TimeoutSec 2
    $taskApiRunning = $taskHealth.service -eq 'Favo.Books.Api'
} catch { }
if (-not $taskApiRunning) {
    Start-FavoTerminal 'Favo Books API' $taskRoot 'dotnet run --project ./src/backend/Api/Favo.Books.Api --launch-profile http'
}

$taskReady = $false
for ($taskAttempt = 0; $taskAttempt -lt 60; $taskAttempt++) {
    try {
        $taskHealth = Invoke-RestMethod 'http://localhost:5117/' -TimeoutSec 2
        if ($taskHealth.service -eq 'Favo.Books.Api') { $taskReady = $true; break }
    } catch { }
    Start-Sleep -Seconds 1
}
if (-not $taskReady) { throw 'API did not start. Check the Favo Books API terminal.' }
Start-FavoTerminal 'Favo Books Web' $taskFrontend 'npm run dev -- --port 5173 --strictPort --open'
