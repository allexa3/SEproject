param(
    [int]$WorkerPort = 7070,
    [int]$MvcPort = 5163
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "Stopping any running instances..."
Get-Process ImagePlatform.WorkerHost | Stop-Process -Force
Get-Process HelloWorldMVC | Stop-Process -Force
Get-Process dotnet | Stop-Process -Force

$ErrorActionPreference = "Stop"

Write-Host "Freeing ports $WorkerPort and $MvcPort ..."
& "$PSScriptRoot\free-port.ps1" -Port $WorkerPort
& "$PSScriptRoot\free-port.ps1" -Port $MvcPort

Write-Host "Starting WorkerHost on http://localhost:$WorkerPort ..."
Start-Process dotnet -ArgumentList @(
    "run",
    "--project", "src/worker/ImagePlatform.WorkerHost/ImagePlatform.WorkerHost.csproj",
    "--urls", "http://localhost:$WorkerPort"
) -WorkingDirectory (Get-Location) | Out-Null

Write-Host "Starting MVC app on http://localhost:$MvcPort ..."
Start-Process dotnet -ArgumentList @(
    "run",
    "--project", "HelloWorldMVC/HelloWorldMVC.csproj",
    "--urls", "http://localhost:$MvcPort"
) -WorkingDirectory (Get-Location) | Out-Null

Write-Host "Done."
Write-Host "Open: http://localhost:$MvcPort/"


