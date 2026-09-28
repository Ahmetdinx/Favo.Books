$ErrorActionPreference = 'Stop'
$root = 'artifacts/reading-list-100-20260928/publisher/pages'
$rows = Get-Content 'artifacts/reading-list-100-20260928/matches.json' -Raw | ConvertFrom-Json
foreach ($row in $rows) {
    if ($row.Number -in 7,11) { continue }
    $text = $row.Title.ToLowerInvariant().Replace('ı','i').Replace([string][char]39,'').Replace([string][char]8217,'').Normalize([Text.NormalizationForm]::FormD)
    $text = -join ($text.ToCharArray() | Where-Object { [Globalization.CharUnicodeInfo]::GetUnicodeCategory($_) -ne 'NonSpacingMark' })
    $slug = [regex]::Replace($text,'[^a-z0-9]+','-').Trim('-')
    $url = "https://www.iskultur.com.tr/$slug.aspx"
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($url))).ToLowerInvariant()
    $path = Join-Path $root "$hash.html"
    if (Test-Path -LiteralPath $path) { continue }
    try {
        $response = Invoke-WebRequest $url -TimeoutSec 20
        [IO.File]::WriteAllText([IO.Path]::GetFullPath($path), $response.Content)
        [IO.File]::WriteAllText([IO.Path]::GetFullPath((Join-Path $root "$hash.url")), $url)
        Write-Output "$($row.Number) OK"
    } catch { Write-Output "$($row.Number) $($_.Exception.Message)" }
    Start-Sleep -Milliseconds 1000
}
