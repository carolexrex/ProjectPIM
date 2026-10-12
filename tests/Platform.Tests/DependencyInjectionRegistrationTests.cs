using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Abstractions.Security;
using Platform.Application.Cart;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Tax;
using Platform.Application.Orders;
using Platform.Application.Storefront;
using Platform.Infrastructure.Catalog.Inventory;
using Platform.Infrastructure.Catalog.Tax;
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

    [Theory]
    [InlineData("InMemory", typeof(InMemoryVatRuleRepository))]
    [InlineData("PostgreSql", typeof(EfVatRuleRepository))]
    public void AddCatalogPersistence_ResolvesVatRuleServicesForProvider(string provider, Type expectedRepositoryType)
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

        Assert.IsType(expectedRepositoryType, scope.ServiceProvider.GetRequiredService<IVatRuleRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IVatRuleAdminApplicationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICartAdminApplicationService>());
    }

    [Theory]
    [InlineData("InMemory", typeof(InMemoryInventoryAllocationRepository), typeof(InMemoryInventoryWriteCoordinator))]
    [InlineData("PostgreSql", typeof(EfInventoryAllocationRepository), typeof(EfInventoryWriteCoordinator))]
    public void AddCatalogPersistence_ResolvesInventoryReservationServicesForProvider(string provider, Type expectedRepositoryType, Type expectedCoordinatorType)
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

        Assert.IsType(expectedRepositoryType, scope.ServiceProvider.GetRequiredService<IInventoryAllocationRepository>());
        Assert.IsType(expectedCoordinatorType, scope.ServiceProvider.GetRequiredService<IInventoryWriteCoordinator>());
        Assert.IsType<InventoryReservationService>(scope.ServiceProvider.GetRequiredService<IInventoryReservationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOrderAdminApplicationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IInventoryAdminApplicationService>());
    }

    private sealed class TestCurrentActorAccessor : ICurrentActorAccessor
    {
        public AuthenticatedActor GetCurrentActor()
        {
            return new AuthenticatedActor("test-user", "Test User", "AdminUser", [], true);
        }
    }
}
