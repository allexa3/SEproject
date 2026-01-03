param(
    [Parameter(Mandatory = $true)]
    [int]$Port
)

$ErrorActionPreference = "SilentlyContinue"

# Find PIDs listening on the port and kill them.
$pids = @()

try {
    $pids = Get-NetTCPConnection -LocalPort $Port -State Listen |
        Select-Object -ExpandProperty OwningProcess -Unique
} catch {
    # Fallback if Get-NetTCPConnection isn't available
    $pids = (netstat -ano | Select-String (":$Port\s+.*LISTENING\s+(\d+)$") | ForEach-Object {
        [int]($_.Matches[0].Groups[1].Value)
    }) | Sort-Object -Unique
}

foreach ($pid in $pids) {
    try {
        Stop-Process -Id $pid -Force
        Write-Host "Stopped PID $pid using port $Port"
    } catch {
        Write-Host "Failed to stop PID $pid (port $Port)"
    }
}

if (-not $pids -or $pids.Count -eq 0) {
    Write-Host "No listener found on port $Port"
}


