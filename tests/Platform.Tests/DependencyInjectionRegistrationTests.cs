using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Abstractions.Security;
using Platform.Infrastructure.DependencyInjection;

namespace Platform.Tests;

public sealed class DependencyInjectionRegistrationTests
{
    [Fact]
    public void AddCatalogPersistence_DoesNotOverridePreRegisteredActorAccessor_ForInMemory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentActorAccessor, TestCurrentActorAccessor>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "InMemory"
            })
            .Build();

        services.AddCatalogPersistence(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var accessor = serviceProvider.GetRequiredService<ICurrentActorAccessor>();

        Assert.IsType<TestCurrentActorAccessor>(accessor);
    }

    [Fact]
    public void AddCatalogPersistence_DoesNotOverridePreRegisteredActorAccessor_ForPostgreSql()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentActorAccessor, TestCurrentActorAccessor>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "PostgreSql",
                ["ConnectionStrings:Platform"] = "Host=localhost;Database=platform_test;Username=test;Password=test"
            })
            .Build();

        services.AddCatalogPersistence(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var accessor = serviceProvider.GetRequiredService<ICurrentActorAccessor>();

        Assert.IsType<TestCurrentActorAccessor>(accessor);
    }

    private sealed class TestCurrentActorAccessor : ICurrentActorAccessor
    {
        public AuthenticatedActor GetCurrentActor()
        {
            return new AuthenticatedActor("test-user", "Test User", "AdminUser", [], true);
        }
    }
}
