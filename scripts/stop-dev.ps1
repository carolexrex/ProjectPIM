param(
    [switch]$KeepDatabase
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$pidRoot = Join-Path $repoRoot ".dev-runtime\pids"
$postgresContainerName = "projektpim-postgres"

function Get-TrackedProcessId {
    param([string]$PidFile)

    if (-not (Test-Path $PidFile)) {
        return $null
    }

    $value = (Get-Content -Path $PidFile -Raw).Trim()
    $parsedId = 0
    if ([int]::TryParse($value, [ref]$parsedId)) {
        return $parsedId
    }

    Remove-Item -LiteralPath $PidFile -Force
    return $null
}

function Stop-TrackedProcess {
    param([string]$Name)

    $pidFile = Join-Path $pidRoot "$Name.pid"
    $managedPid = Get-TrackedProcessId -PidFile $pidFile
    if (-not $managedPid) {
        Write-Host "$Name is not tracked."
        return
    }

    try {
        $process = Get-Process -Id $managedPid -ErrorAction Stop
        Write-Host "Stopping $Name (pid $managedPid)"
        Stop-Process -Id $process.Id -Force
    }
    catch {
        Write-Host "$Name pid $managedPid is not running."
    }
    finally {
        if (Test-Path $pidFile) {
            Remove-Item -LiteralPath $pidFile -Force
        }
    }
}

Stop-TrackedProcess -Name "api"
Stop-TrackedProcess -Name "storefront-api"
Stop-TrackedProcess -Name "backoffice"
Stop-TrackedProcess -Name "worker"

if (-not $KeepDatabase) {
    $existingName = (& docker ps -a --format "{{.Names}}" | Where-Object { $_ -eq $postgresContainerName } | Select-Object -First 1)
    if (-not [string]::IsNullOrWhiteSpace($existingName)) {
        $isRunning = (& docker inspect -f "{{.State.Running}}" $postgresContainerName).Trim()
        if ($LASTEXITCODE -eq 0 -and $isRunning -eq "true") {
            Write-Host "Stopping PostgreSQL container '$postgresContainerName'"
            & docker stop $postgresContainerName | Out-Null
        }
    }
}
