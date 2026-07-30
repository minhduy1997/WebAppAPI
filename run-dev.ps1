# Run API without launchSettings (avoids Smart App Control blocking rebuilt binaries).
# Usage: .\run-dev.ps1
$ErrorActionPreference = "Stop"
$env:ASPNETCORE_ENVIRONMENT = "Development"
Set-Location $PSScriptRoot
dotnet run --no-launch-profile --urls http://localhost:5259
