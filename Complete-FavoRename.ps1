$ErrorActionPreference = 'Stop'
$taskSource = [IO.Path]::GetFullPath($PSScriptRoot)
$taskParent = Split-Path -Parent $taskSource
$taskTarget = [IO.Path]::GetFullPath((Join-Path $taskParent 'Favo Kitap'))
if ((Split-Path -Leaf $taskSource) -eq 'Favo Kitap') {
    Write-Host "Already renamed: $taskSource"
    exit 0
}
if ((Split-Path -Leaf $taskSource) -notin @('KitapKivo', 'Favo.Books') -or
    (Split-Path -Parent $taskTarget) -ne $taskParent -or
    -not (Test-Path -LiteralPath (Join-Path $taskSource 'Favo.Books.slnx'))) {
    throw 'Unexpected source or destination. No changes made.'
}
if (Test-Path -LiteralPath $taskTarget) { throw "Destination already exists: $taskTarget" }
# Release this terminal's working-directory handle before renaming.
Set-Location -LiteralPath $taskParent
try {
    Move-Item -LiteralPath $taskSource -Destination $taskTarget -ErrorAction Stop
} catch {
    throw 'Folder is still in use. Save and close Codex, Visual Studio, and terminals in KitapKivo, then retry from the parent directory.'
}
Write-Host "Renamed: $taskTarget"
Write-Host "Open $taskTarget\Favo.Books.slnx in Visual Studio."
Write-Host "Open $taskTarget as the project folder in Codex."
Write-Host 'Run .\Start-Favo.ps1 from the new folder to start the application.'
