$ErrorActionPreference = "Stop"

Write-Host "CellVera Release Builder" -ForegroundColor Cyan
Write-Host ""

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host "ERROR: The .NET CLI was not found." -ForegroundColor Red
    Write-Host "Install the .NET 8 SDK (or the .NET desktop workload in Visual Studio), then reopen PowerShell." -ForegroundColor Yellow
    exit 1
}

$sdkList = @(& dotnet --list-sdks)
if ($LASTEXITCODE -ne 0 -or $sdkList.Count -eq 0) {
    Write-Host "ERROR: No .NET SDK is installed." -ForegroundColor Red
    Write-Host "A runtime alone is not enough to build CellVera." -ForegroundColor Yellow
    exit 1
}

Write-Host "Detected .NET SDK(s):" -ForegroundColor Green
$sdkList | ForEach-Object { Write-Host "  $_" }
Write-Host ""

Push-Location $PSScriptRoot
try {
    Write-Host "Restoring packages..." -ForegroundColor Cyan
    & dotnet restore .\CellVera.csproj
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }

    # Publish to a fresh folder so a running older CellVera.exe cannot block the build.
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $publishDir = Join-Path $PSScriptRoot "dist\CellVera-$stamp"

    Write-Host ""
    Write-Host "Publishing self-contained Windows x64 app..." -ForegroundColor Cyan
    & dotnet publish .\CellVera.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    $exePath = Join-Path $publishDir "CellVera.exe"
    if (-not (Test-Path $exePath)) {
        throw "Publish completed but CellVera.exe was not found at: $exePath"
    }

    Write-Host ""
    Write-Host "Build complete." -ForegroundColor Green
    Write-Host "Executable:" -ForegroundColor Green
    Write-Host "  $exePath" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "BUILD FAILED" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
