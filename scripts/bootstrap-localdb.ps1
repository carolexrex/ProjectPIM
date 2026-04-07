param(
    [string]$ServerInstance = "localhost",
    [string]$DatabaseName = "ProjektPim",
    [string]$Username = "",
    [string]$Password = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$schemaScript = Join-Path $repoRoot "sql\\001_initial_schema.sql"
$seedScript = Join-Path $repoRoot "sql\\002_seed_baseline.sql"

if (-not (Test-Path $schemaScript)) {
    throw "Schema script not found: $schemaScript"
}

if (-not (Test-Path $seedScript)) {
    throw "Seed script not found: $seedScript"
}

$sqlcmdPath = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE"
if (-not (Test-Path $sqlcmdPath)) {
    throw "sqlcmd was not found at: $sqlcmdPath"
}

$authArgs = if ([string]::IsNullOrWhiteSpace($Username)) {
    @("-E")
} else {
    if ([string]::IsNullOrWhiteSpace($Password)) {
        throw "When -Username is provided, -Password is also required."
    }

    @("-U", $Username, "-P", $Password)
}

$createDatabaseCommand = @"
IF DB_ID(N'$DatabaseName') IS NULL
BEGIN
    CREATE DATABASE [$DatabaseName];
END
"@

& $sqlcmdPath -S $ServerInstance @authArgs -C -d master -b -Q $createDatabaseCommand
if ($LASTEXITCODE -ne 0) {
    throw "Database creation failed."
}

& $sqlcmdPath -S $ServerInstance @authArgs -C -d $DatabaseName -b -i $schemaScript
if ($LASTEXITCODE -ne 0) {
    throw "Schema bootstrap failed."
}

& $sqlcmdPath -S $ServerInstance @authArgs -C -d $DatabaseName -b -i $seedScript
if ($LASTEXITCODE -ne 0) {
    throw "Seed bootstrap failed."
}

Write-Host "Bootstrapped database '$DatabaseName' on '$ServerInstance'."
