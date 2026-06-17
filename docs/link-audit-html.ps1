<#
.SYNOPSIS
  Scans all .html files in the generated _site/ for relative links and reports broken ones.
  Usage: .\link-audit-html.ps1 [-siteDir <path>]
#>

param(
    [string]$siteDir = ""
)

if (-not $siteDir) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $siteDir = Resolve-Path (Join-Path $scriptDir "..\_site")
}

Write-Host "Auditing: $siteDir" -ForegroundColor Cyan

$allHtml = Get-ChildItem -Path $siteDir -Filter *.html -Recurse
$broken = @()
$linkRegex = [regex] '(?:href|src)="([^"]+)"'
$skipPrefixes = @('http://', 'https://', 'mailto:', 'tel:', 'ftp://', '//cdn.', '//fonts.')

$total = $allHtml.Count
$count = 0

foreach ($file in $allHtml) {
    $count++
    if ($count % 20 -eq 0) { Write-Host "  [$count/$total] $($file.Name)" -ForegroundColor DarkGray }

    $content = Get-Content -LiteralPath $file.FullName -Raw
    $dir = $file.Directory.FullName
    $matches = $linkRegex.Matches($content)
    $seen = @{}

    foreach ($match in $matches) {
        $rawTarget = $match.Groups[1].Value.Trim()
        $target = $rawTarget -replace '#.*$', '' -replace '/$', ''
        if ([string]::IsNullOrWhiteSpace($target)) { continue }

        $skip = $false
        foreach ($prefix in $skipPrefixes) {
            if ($target.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { $skip = $true; break }
        }
        if ($skip) { continue }
        if ($target.StartsWith('#')) { continue }

        $key = $target.ToLowerInvariant()
        if ($seen.ContainsKey($key)) { continue }
        $seen[$key] = $true

        try {
            $fullPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($dir, $target))
            if (Test-Path -LiteralPath $fullPath -PathType Leaf) { continue }
            if (Test-Path -LiteralPath $fullPath -PathType Container) { continue }
            $broken += [PSCustomObject]@{
                File = $file.FullName.Substring($siteDir.Length + 1)
                Link = $rawTarget
            }
        } catch {
            $broken += [PSCustomObject]@{
                File = $file.FullName.Substring($siteDir.Length + 1)
                Link = "$rawTarget (resolve error: $($_.Exception.Message))"
            }
        }
    }
}

if ($broken.Count -eq 0) {
    Write-Host "All links OK. No broken relative links found in _site/ output." -ForegroundColor Green
} else {
    Write-Host "Found $($broken.Count) broken link(s):" -ForegroundColor Yellow
    $broken | Format-Table -AutoSize File, Link
}
