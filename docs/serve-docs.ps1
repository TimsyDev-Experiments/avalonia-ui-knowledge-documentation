param(
    [int]$Port = 8080,
    [switch]$NoBrowser
)

$DocsDir = Split-Path -Parent $PSCommandPath
$HostedUrl = "http://localhost:$Port"

# Try dotnet serve first, then python, then http-server
$tool = $null

if (Get-Command "dotnet" -ErrorAction SilentlyContinue) {
    $installed = dotnet tool list --global 2>$null | Select-String "dotnet-serve"
    if ($installed) {
        $tool = "dotnet-serve"
    } else {
        Write-Host "dotnet-serve not found. Installing..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-serve 2>$null
        if ($LASTEXITCODE -eq 0) { $tool = "dotnet-serve" }
    }
}

if (-not $tool -and (Get-Command "python" -ErrorAction SilentlyContinue)) {
    $tool = "python"
}

if (-not $tool) {
    Write-Host @"

No HTTP server found. Install one:

  dotnet tool install -g dotnet-serve

Or install Python from https://python.org

"@ -ForegroundColor Red
    exit 1
}

if (-not $NoBrowser) {
    Start-Process $HostedUrl
}

Write-Host @"

  Avalonia Docs Viewer
  ─────────────────────────────────────
  URL:         $HostedUrl
  Viewer:      $HostedUrl/viewer.html
  Serving:     $DocsDir
  Tool:        $tool
  Press Ctrl+C to stop

"@ -ForegroundColor Cyan

switch ($tool) {
    "dotnet-serve" {
        dotnet serve --directory $DocsDir --port $Port
    }
    "python" {
        Push-Location $DocsDir
        python -m http.server $Port
        Pop-Location
    }
}
