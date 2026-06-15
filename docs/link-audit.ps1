<#
.SYNOPSIS
  Scans all .md files in the docs tree for relative links and reports broken ones.
#>

$docsRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$allMd = Get-ChildItem -Path $docsRoot -Filter *.md -Recurse
$broken = @()
$linkRegex = [regex] '\[([^\]]*)\]\(([^)]+)\)'
$skipPrefixes = @('http://', 'https://', 'mailto:', 'tel:', 'ftp://')

foreach ($file in $allMd) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $dir = $file.Directory.FullName
    $matches = $linkRegex.Matches($content)
    $seen = @{}

    foreach ($match in $matches) {
        $rawTarget = $match.Groups[2].Value.Trim()
        $target = $rawTarget -replace '#.*$', '' -replace '/$', ''
        if ([string]::IsNullOrWhiteSpace($target)) { continue }

        $skip = $false
        foreach ($prefix in $skipPrefixes) {
            if ($target.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { $skip = $true; break }
        }
        if ($skip) { continue }
        if ($target -match '^\$') { continue }
        if ($target -match '^file://') { continue }
        if ($target -match '^#') { continue }

        $key = $target.ToLowerInvariant()
        if ($seen.ContainsKey($key)) { continue }
        $seen[$key] = $true

        try {
            $fullPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($dir, $target))
            if (Test-Path -LiteralPath $fullPath -PathType Leaf) { continue }
            if (-not [System.IO.Path]::HasExtension($target)) {
                $withMd = $fullPath + '.md'
                if (Test-Path -LiteralPath $withMd -PathType Leaf) { continue }
            }
            if (Test-Path -LiteralPath $fullPath -PathType Container) { continue }
            $broken += [PSCustomObject]@{
                File = $file.FullName.Substring($docsRoot.Length + 1)
                Link = $rawTarget
            }
        } catch {
            $broken += [PSCustomObject]@{
                File = $file.FullName.Substring($docsRoot.Length + 1)
                Link = "$rawTarget (resolve error: $($_.Exception.Message))"
            }
        }
    }
}

if ($broken.Count -eq 0) {
    Write-Host "All links OK. No broken relative links found." -ForegroundColor Green
} else {
    Write-Host "Found $($broken.Count) broken link(s):" -ForegroundColor Yellow
    $broken | Format-Table -AutoSize File, Link
}
