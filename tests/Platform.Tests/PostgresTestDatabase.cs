using Microsoft.EntityFrameworkCore;
using Npgsql;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Security;

namespace Platform.Tests;

/// <summary>
/// Runs a test only when <c>PLATFORM_TEST_POSTGRES</c> holds a server connection string (no database),
/// for example <c>Host=localhost;Port=55438;Username=postgres;Password=postgres</c>. The account must be
/// allowed to create and drop databases.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(PostgresTestDatabase.ServerConnectionString))
        {
            Skip = $"Set {PostgresTestDatabase.EnvironmentVariable} to run PostgreSQL concurrency tests.";
        }
    }
}

/// <summary>
/// Migrates one template database per test class, then clones a fresh database from it for each
/// test. All databases created here are dropped on dispose.
/// </summary>
public sealed class PostgresTestDatabase : IAsyncLifetime
{
    public const string EnvironmentVariable = "PLATFORM_TEST_POSTGRES";

    private readonly List<string> _databases = [];
    private string? _templateName;

    public static string? ServerConnectionString => Environment.GetEnvironmentVariable(EnvironmentVariable);

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(ServerConnectionString))
        {
            return;
        }

        _templateName = $"platform_test_tpl_{Guid.NewGuid():N}";
        await ExecuteAsync($"CREATE DATABASE \"{_templateName}\"");
        _databases.Add(_templateName);

        await using (var db = CreateContext(ConnectionStringFor(_templateName)))
        {
            await db.Database.MigrateAsync();
        }

        // A template cannot be cloned while connections to it are open.
        NpgsqlConnection.ClearAllPools();
    }

    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"platform_test_{Guid.NewGuid():N}";
        await ExecuteAsync($"CREATE DATABASE \"{name}\" TEMPLATE \"{_templateName}\"");
        _databases.Add(name);
        return ConnectionStringFor(name);
    }

    public static PlatformDbContext CreateContext(string connectionString)
    {
        return new PlatformDbContext(
            new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connectionString).Options,
            new SystemCurrentActorAccessor());
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(ServerConnectionString))
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        foreach (var name in Enumerable.Reverse(_databases))
        {
            await ExecuteAsync($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
        }
    }

    private static string ConnectionStringFor(string database)
    {
        return new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = database }.ConnectionString;
    }

    private static async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionStringFor("postgres"));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
