# Local Database Bootstrap

The current development path is SQL Server.

## Default development settings

`src/Platform.Api/appsettings.Development.json` now uses:

- `Persistence:Provider = SqlServer`
- `ConnectionStrings:Platform = Server=localhost;Database=ProjektPim;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true`

## Intended authentication strategy

Use different database auth strategies for different environments:

- local development: Windows authentication
- self-hosted production: SQL authentication with a dedicated application login
- managed cloud production later: managed identity / token-based auth if the platform supports it

This means:

- local development can use your own Windows account for convenience
- production should not depend on an interactive Windows login
- production should not use `sa`
- production runtime credentials and migration credentials should be separated

## Bootstrap the database

Run the local bootstrap script from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\bootstrap-localdb.ps1
```

What it does:

1. Creates the `ProjektPim` database if it does not already exist
2. Applies `sql/001_initial_schema.sql`
3. Applies `sql/002_seed_baseline.sql`

The script now uses `sqlcmd`, not `Invoke-Sqlcmd`, because `sqlcmd` is available on this machine and `Invoke-Sqlcmd` is not.

If you want to target a different instance:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\bootstrap-localdb.ps1 -ServerInstance "localhost"
```

or:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\bootstrap-localdb.ps1 -ServerInstance "(localdb)\MSSQLLocalDB"
```

If Windows integrated authentication fails on this machine, use a SQL login instead:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\bootstrap-localdb.ps1 -ServerInstance "localhost" -Username "app_login" -Password "<your-password>"
```

You can also override the API connection string without storing a password in `appsettings.Development.json`:

```powershell
$env:ConnectionStrings__Platform = "Server=localhost;Database=ProjektPim;User ID=app_login;Password=<your-password>;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
```

Recommended production shape:

```powershell
$env:ConnectionStrings__Platform = "Server=sql-prod-host;Database=ProjektPim;User ID=app_login;Password=<your-password>;TrustServerCertificate=False;Encrypt=True;MultipleActiveResultSets=true"
```

## EF design-time support

`src/Platform.Infrastructure/Persistence/DesignTimePlatformDbContextFactory.cs` was added so `dotnet ef` can resolve `PlatformDbContext` from the API configuration when the local SDK/tooling issue is fixed.

Expected next command once tooling is healthy:

```powershell
dotnet ef migrations add InitialCatalog --project .\src\Platform.Infrastructure\Platform.Infrastructure.csproj --startup-project .\src\Platform.Api\Platform.Api.csproj
```

## Current limitation

The repository still has a local `.NET 10` restore/build issue. Also, from this shell environment:

- `Invoke-Sqlcmd` is not installed
- `MSSQLLocalDB` reports a registry/configuration error
- `MSSQLSERVER` is installed with shared memory enabled, TCP off, named pipes off, and mixed-mode auth enabled
- Windows integrated authentication currently fails with `Failed to generate SSPI context`
- `sqlcmd` currently fails before login when using the Windows auth path

So the intended default remains `localhost` with Windows authentication for local development, but I was not able to complete a live end-to-end SQL exercise from this shell environment.
