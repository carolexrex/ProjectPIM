# Local Database Bootstrap

The current development path is PostgreSQL.

## Default development settings

`src/Platform.Api/appsettings.Development.json` now uses:

- `Persistence:Provider = PostgreSql`
- `ConnectionStrings:Platform = Host=localhost;Port=5432;Database=projektpim;Username=postgres;Password=postgres`

## Recommended local setup

Run PostgreSQL locally with Docker:

```powershell
docker run --name projektpim-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres -e POSTGRES_DB=projektpim -p 5432:5432 -d postgres:17
```

## One-command dev startup

To start PostgreSQL, apply migrations, build the solution, and launch both the admin API and backoffice together:

Windows PowerShell:

```powershell
./scripts/start-dev.ps1
```

macOS or Linux:

```bash
bash ./scripts/start-dev.sh
```

Both scripts:

- ensure the `projektpim-postgres` container exists and is running
- wait for PostgreSQL readiness
- apply EF migrations unless you opt out
- build the solution
- start `Platform.Api` on `http://localhost:5053`
- start `Platform.Backoffice` on `http://localhost:5168`
- write logs and pid files under `.dev-runtime/`

Optional flags:

- `-SkipDatabaseStart` or `--skip-db`: skip Docker/container startup if PostgreSQL is already running
- `-SkipMigrate` or `--skip-migrate`: skip EF migration application

To stop the same stack:

Windows PowerShell:

```powershell
./scripts/stop-dev.ps1
```

macOS or Linux:

```bash
bash ./scripts/stop-dev.sh
```

## Connection string override

You can override the API connection string without editing source-controlled config:

```powershell
$env:ConnectionStrings__Platform = "Host=localhost;Port=5432;Database=projektpim;Username=postgres;Password=<your-password>"
```

## EF design-time support

`src/Platform.Infrastructure/Persistence/DesignTimePlatformDbContextFactory.cs` resolves `PlatformDbContext` from API configuration.

Restore local tools if needed:

```powershell
dotnet tool restore
```

Apply the current migrations:

```powershell
dotnet build Platform.slnx -m:1 -nr:false
dotnet tool run dotnet-ef database update --no-build --project .\src\Platform.Infrastructure\Platform.Infrastructure.csproj --startup-project .\src\Platform.Infrastructure\Platform.Infrastructure.csproj --context Platform.Infrastructure.Persistence.PlatformDbContext
```

## Current development status

- The initial PostgreSQL migration and seed migration already exist.
- The solution builds with:

```powershell
$env:DOTNET_CLI_HOME = "C:\Projects\ProjectPIM\.dotnet-cli"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
dotnet build Platform.slnx -m:1 -nr:false
```
