using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Variants;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Persistence:Provider"] ?? "InMemory";

        services.AddScoped<IProductAdminApplicationService, InMemoryProductAdminApplicationService>();
        services.AddScoped<IVariantAdminApplicationService, InMemoryVariantAdminApplicationService>();

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return services.AddSqlServerCatalogPersistence(configuration);
        }

        if (!string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unknown persistence provider '{provider}'.");
        }

        return services.AddInMemoryCatalogPersistence();
    }

    private static IServiceCollection AddInMemoryCatalogPersistence(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryCatalogStore>();
        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();
        services.AddScoped<IProductRepository, InMemoryProductRepository>();
        services.AddScoped<IProductStatusDefinitionRepository, InMemoryProductStatusDefinitionRepository>();
        services.AddScoped<IVariantRepository, InMemoryVariantRepository>();
        return services;
    }

    private static IServiceCollection AddSqlServerCatalogPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Platform");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Platform' is required when Persistence:Provider is set to 'SqlServer'.");
        }

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IProductStatusDefinitionRepository, EfProductStatusDefinitionRepository>();
        services.AddScoped<IVariantRepository, EfVariantRepository>();

        return services;
    }
}
