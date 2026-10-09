using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Abstractions.Security;
using Platform.Application.Storefront;
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

    [Theory]
    [InlineData("InMemory")]
    [InlineData("PostgreSql")]
    public void AddCatalogPersistence_ResolvesTimeDrivenStorefrontProjectionServices(string provider)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = provider,
                ["ConnectionStrings:Platform"] = "Host=localhost;Database=platform_test;Username=test;Password=test"
            })
            .Build();

        services.AddCatalogPersistence(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStorefrontProjectionBuilder>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStorefrontPriceValidityScanRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStorefrontPriceValidityRefreshService>());
    }

    private sealed class TestCurrentActorAccessor : ICurrentActorAccessor
    {
        public AuthenticatedActor GetCurrentActor()
        {
            return new AuthenticatedActor("test-user", "Test User", "AdminUser", [], true);
        }
    }
}
