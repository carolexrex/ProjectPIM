param(
    [switch]$SkipDatabaseStart,
    [switch]$SkipMigrate,
    [switch]$OpenBackoffice
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$runtimeRoot = Join-Path $repoRoot ".dev-runtime"
$pidRoot = Join-Path $runtimeRoot "pids"
$logRoot = Join-Path $runtimeRoot "logs"
$dotnetCliHome = Join-Path $repoRoot ".dotnet-cli"

$apiProjectRoot = Join-Path $repoRoot "src\Platform.Api"
$storefrontApiProjectRoot = Join-Path $repoRoot "src\Platform.StorefrontApi"
$backofficeProjectRoot = Join-Path $repoRoot "src\Platform.Backoffice"
$workerProjectRoot = Join-Path $repoRoot "src\Platform.Worker"
$infrastructureProject = Join-Path $repoRoot "src\Platform.Infrastructure\Platform.Infrastructure.csproj"
$solutionPath = Join-Path $repoRoot "Platform.slnx"
$apiDll = Join-Path $apiProjectRoot "bin\Debug\net10.0\Platform.Api.dll"
$storefrontApiDll = Join-Path $storefrontApiProjectRoot "bin\Debug\net10.0\Platform.StorefrontApi.dll"
$backofficeDll = Join-Path $backofficeProjectRoot "bin\Debug\net10.0\Platform.Backoffice.dll"
$workerDll = Join-Path $workerProjectRoot "bin\Debug\net10.0\Platform.Worker.dll"

$apiUrl = "http://localhost:5053/"
$storefrontApiUrl = "http://localhost:5064/"
$backofficeUrl = "http://localhost:5168/"
$apiProbeUrl = $apiUrl
$storefrontApiProbeUrl = $storefrontApiUrl
$backofficeProbeUrl = "${backofficeUrl}auth/login"
$postgresContainerName = "projektpim-postgres"
$postgresImage = "postgres:17"

New-Item -ItemType Directory -Force -Path $runtimeRoot, $pidRoot, $logRoot, $dotnetCliHome | Out-Null

$env:DOTNET_CLI_HOME = $dotnetCliHome
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

function Write-Step {
    param([string]$Message)

    Write-Host "==> $Message"
}

function Assert-Command {
    param([string]$CommandName)

    if (-not (Get-Command $CommandName -ErrorAction SilentlyContinue)) {
        throw "Required command '$CommandName' was not found in PATH."
    }
}

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$Description
    )

    Write-Step $Description
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

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

function Test-ProcessAlive {
    param([int]$ProcessId)

    try {
        Get-Process -Id $ProcessId -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Start-PostgresContainer {
    $existingName = (& docker ps -a --format "{{.Names}}" | Where-Object { $_ -eq $postgresContainerName } | Select-Object -First 1)

    if ([string]::IsNullOrWhiteSpace($existingName)) {
        Write-Step "Creating PostgreSQL container '$postgresContainerName'"
        & docker run `
            --name $postgresContainerName `
            -e POSTGRES_PASSWORD=postgres `
            -e POSTGRES_USER=postgres `
            -e POSTGRES_DB=projektpim `
            -p 5432:5432 `
            -d `
            $postgresImage | Out-Null

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create PostgreSQL container '$postgresContainerName'."
        }
    }
    else {
        $isRunning = (& docker inspect -f "{{.State.Running}}" $postgresContainerName).Trim()
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to inspect PostgreSQL container '$postgresContainerName'."
        }

        if ($isRunning -ne "true") {
            Write-Step "Starting PostgreSQL container '$postgresContainerName'"
            & docker start $postgresContainerName | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to start PostgreSQL container '$postgresContainerName'."
            }
        }
        else {
            Write-Step "PostgreSQL container '$postgresContainerName' is already running"
        }
    }

    Write-Step "Waiting for PostgreSQL readiness"
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        & docker exec $postgresContainerName pg_isready -U postgres -d projektpim | Out-Null
        if ($LASTEXITCODE -eq 0) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "PostgreSQL container '$postgresContainerName' did not become ready within 60 seconds."
}

function Start-ManagedDotnetProcess {
    param(
        [string]$Name,
        [string]$WorkingDirectory,
        [string]$ApplicationDll,
        [string]$Url = ""
    )

    $pidFile = Join-Path $pidRoot "$Name.pid"
    $stdoutLog = Join-Path $logRoot "$Name.out.log"
    $stderrLog = Join-Path $logRoot "$Name.err.log"

    $existingPid = Get-TrackedProcessId -PidFile $pidFile
    if ($existingPid -and (Test-ProcessAlive -ProcessId $existingPid)) {
        Write-Step "$Name is already running with pid $existingPid"
        return
    }

    if (Test-Path $pidFile) {
        Remove-Item -LiteralPath $pidFile -Force
    }

    Write-Step "Starting $Name"
    $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $previousDotnetEnvironment = $env:DOTNET_ENVIRONMENT
    $previousUrls = $env:ASPNETCORE_URLS
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:DOTNET_ENVIRONMENT = "Development"
    if (-not [string]::IsNullOrWhiteSpace($Url)) {
        $env:ASPNETCORE_URLS = $Url
    }

    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @($ApplicationDll) `
        -WorkingDirectory $WorkingDirectory `
        -RedirectStandardOutput $stdoutLog `
        -RedirectStandardError $stderrLog `
        -PassThru

    if ($null -eq $previousEnvironment) {
        Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    }

    if ($null -eq $previousDotnetEnvironment) {
        Remove-Item Env:DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:DOTNET_ENVIRONMENT = $previousDotnetEnvironment
    }

    if ($null -eq $previousUrls) {
        Remove-Item Env:ASPNETCORE_URLS -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_URLS = $previousUrls
    }

    Set-Content -Path $pidFile -Value $process.Id -NoNewline
}

function Wait-ForHttpEndpoint {
    param(
        [string]$Name,
        [string]$Url
    )

    $pidFile = Join-Path $pidRoot "$Name.pid"
    $stdoutLog = Join-Path $logRoot "$Name.out.log"
    $stderrLog = Join-Path $logRoot "$Name.err.log"

    $curlCommand = Get-Command "curl.exe" -ErrorAction SilentlyContinue

    Write-Step "Waiting for $Name on $Url"
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($curlCommand) {
            $statusCode = (& $curlCommand.Source --silent --output NUL --write-out "%{http_code}" --max-time 5 $Url).Trim()
            if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($statusCode) -and $statusCode -ne "000") {
                return
            }
        }
        else {
            try {
                Invoke-WebRequest -Uri $Url -Method Get -MaximumRedirection 0 -TimeoutSec 5 | Out-Null
                return
            }
            catch {
                if ($_.Exception.Response) {
                    return
                }
            }
        }

        $managedPid = Get-TrackedProcessId -PidFile $pidFile
        if ($managedPid -and -not (Test-ProcessAlive -ProcessId $managedPid)) {
            $stdoutTail = if (Test-Path $stdoutLog) { (Get-Content $stdoutLog -Tail 20) -join [Environment]::NewLine } else { "<no stdout log>" }
            $stderrTail = if (Test-Path $stderrLog) { (Get-Content $stderrLog -Tail 20) -join [Environment]::NewLine } else { "<no stderr log>" }

            throw "$Name exited before becoming reachable.`nSTDOUT:`n$stdoutTail`n`nSTDERR:`n$stderrTail"
        }

        Start-Sleep -Seconds 1
    }

    throw "$Name did not become reachable on $Url within 60 seconds."
}

Assert-Command -CommandName "docker"
Assert-Command -CommandName "dotnet"

if (-not $SkipDatabaseStart) {
    Start-PostgresContainer
}
else {
    Write-Step "Skipping PostgreSQL container startup"
}

if (-not $SkipMigrate) {
    Invoke-Checked -FilePath "dotnet" -Arguments @("tool", "restore") -Description "Restoring local dotnet tools"
}

Invoke-Checked -FilePath "dotnet" -Arguments @("build", $solutionPath, "-m:1", "-nr:false") -Description "Building solution"

if (-not $SkipMigrate) {
    Invoke-Checked `
        -FilePath "dotnet" `
        -Arguments @(
            "tool", "run", "dotnet-ef", "database", "update",
            "--no-build",
            "--project", $infrastructureProject,
            "--startup-project", $infrastructureProject,
            "--context", "Platform.Infrastructure.Persistence.PlatformDbContext"
        ) `
        -Description "Applying PostgreSQL migrations"
}

if (-not (Test-Path $apiDll)) {
    throw "Built API assembly was not found at '$apiDll'."
}

if (-not (Test-Path $storefrontApiDll)) {
    throw "Built Storefront API assembly was not found at '$storefrontApiDll'."
}

if (-not (Test-Path $backofficeDll)) {
    throw "Built Backoffice assembly was not found at '$backofficeDll'."
}

if (-not (Test-Path $workerDll)) {
    throw "Built Worker assembly was not found at '$workerDll'."
}

Start-ManagedDotnetProcess -Name "api" -WorkingDirectory $apiProjectRoot -ApplicationDll $apiDll -Url $apiUrl
Start-ManagedDotnetProcess -Name "storefront-api" -WorkingDirectory $storefrontApiProjectRoot -ApplicationDll $storefrontApiDll -Url $storefrontApiUrl
Start-ManagedDotnetProcess -Name "backoffice" -WorkingDirectory $backofficeProjectRoot -ApplicationDll $backofficeDll -Url $backofficeUrl
Start-ManagedDotnetProcess -Name "worker" -WorkingDirectory $workerProjectRoot -ApplicationDll $workerDll

Wait-ForHttpEndpoint -Name "api" -Url $apiProbeUrl
Wait-ForHttpEndpoint -Name "storefront-api" -Url $storefrontApiProbeUrl
Wait-ForHttpEndpoint -Name "backoffice" -Url $backofficeProbeUrl

Write-Host ""
Write-Host "Development stack is running."
Write-Host "Admin API:      $apiUrl"
Write-Host "Storefront API: $storefrontApiUrl"
Write-Host "Backoffice:     $backofficeUrl"
Write-Host "Worker:         running"
Write-Host "Logs:           $logRoot"
Write-Host "Stop with:      ./scripts/stop-dev.ps1"

if ($OpenBackoffice) {
    Start-Process $backofficeUrl | Out-Null
}
