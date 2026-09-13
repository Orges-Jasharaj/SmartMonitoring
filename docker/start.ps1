# Starts the full SmartMonitoring stack and waits until health-checked services are ready.
# Prefer this over foreground "docker compose up" (Ctrl+C stops every container).
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot
try {
    docker compose up -d --wait
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose up failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "SmartMonitoring stack is up:" -ForegroundColor Green
    docker compose ps
}
finally {
    Pop-Location
}
