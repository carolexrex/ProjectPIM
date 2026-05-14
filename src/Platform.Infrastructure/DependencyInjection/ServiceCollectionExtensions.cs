using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Abstractions.Security;
using Platform.Application.Auditing;
using Platform.Application.Catalog.Attributes;
using Platform.Application.Catalog.Brands;
using Platform.Application.Catalog.Inventory;
using Platform.Application.Catalog.Channels;
using Platform.Application.Catalog.Categories;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Media;
using Platform.Application.Catalog.Pricing;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Variants;
using Platform.Application.Cart;
using Platform.Application.Companies;
using Platform.Application.Customers;
using Platform.Application.Integrations;
using Platform.Application.Orders;
using Platform.Application.Security.AdminUsers;
using Platform.Application.Storefront;
using Platform.Infrastructure.Auditing;
using Platform.Infrastructure.Cart;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Attributes;
using Platform.Infrastructure.Catalog.Brands;
using Platform.Infrastructure.Catalog.Inventory;
using Platform.Infrastructure.Catalog.Channels;
using Platform.Infrastructure.Catalog.Categories;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Catalog.Media;
using Platform.Infrastructure.Catalog.Pricing;
using Platform.Infrastructure.Catalog.Products;
using Platform.Infrastructure.Catalog.Variants;
using Platform.Infrastructure.Companies;
using Platform.Infrastructure.Customers;
using Platform.Infrastructure.Integrations;
using Platform.Infrastructure.Orders;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Security;
using Platform.Infrastructure.Security.AdminUsers;
using Platform.Infrastructure.Storefront;

namespace Platform.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Persistence:Provider"] ?? "InMemory";

        services.AddOptions<WebhookReplayOptions>()
            .BindConfiguration(WebhookReplayOptions.SectionName)
            .Validate(options => options.ManualReplayDelaySeconds >= 0, "Webhooks:ManualReplayDelaySeconds must be zero or greater.")
            .ValidateOnStart();

        services.AddScoped<IProductAdminApplicationService, InMemoryProductAdminApplicationService>();
        services.AddScoped<IVariantAdminApplicationService, InMemoryVariantAdminApplicationService>();
        services.AddScoped<IBrandAdminApplicationService, BrandAdminApplicationService>();
        services.AddScoped<IAuditLogApplicationService, AuditLogApplicationService>();
        services.AddScoped<IAdminUserAdminApplicationService, AdminUserAdminApplicationService>();
        services.AddScoped<IIntegrationJobAdminApplicationService, IntegrationJobAdminApplicationService>();
        services.AddScoped<IIntegrationJobExecutionService, IntegrationJobExecutionService>();
        services.AddScoped<IWebhookAdminApplicationService, WebhookAdminApplicationService>();
        services.AddScoped<IWebhookOutboxExecutionService, WebhookOutboxExecutionService>();
        services.AddScoped<IWebhookDeliveryExecutionService, WebhookDeliveryExecutionService>();
        services.AddScoped<IOutboxEventPublisher, OutboxEventPublisher>();
        services.AddHttpClient<IWebhookSender, HttpClientWebhookSender>();
        services.AddScoped<CartAdminApplicationService>();
        services.AddScoped<OrderAdminApplicationService>();
        services.AddScoped<ICartAdminApplicationService>(serviceProvider => serviceProvider.GetRequiredService<CartAdminApplicationService>());
        services.AddScoped<IOrderAdminApplicationService>(serviceProvider => serviceProvider.GetRequiredService<OrderAdminApplicationService>());
        services.AddScoped<ICustomerAdminApplicationService, CustomerAdminApplicationService>();
        services.AddScoped<ICompanyAdminApplicationService, CompanyAdminApplicationService>();
        services.AddScoped<IMarketAdminApplicationService, MarketAdminApplicationService>();
        services.AddScoped<IChannelAdminApplicationService, ChannelAdminApplicationService>();
        services.AddScoped<IInventoryAdminApplicationService, InventoryAdminApplicationService>();
        services.AddScoped<IPriceListAdminApplicationService, PriceListAdminApplicationService>();
        services.AddScoped<IStorefrontContextApplicationService, StorefrontContextApplicationService>();
        services.AddScoped<IStorefrontCategoryApplicationService, StorefrontCategoryApplicationService>();
        services.AddScoped<IStorefrontProductApplicationService, StorefrontProductApplicationService>();
        services.AddScoped<IStorefrontProjectionBuilder, StorefrontProjectionBuilder>();
        services.AddScoped<IStorefrontProjectionRefreshService, StorefrontProjectionRefreshService>();
        services.AddScoped<ICategoryAdminApplicationService, InMemoryCategoryAdminApplicationService>();
        services.AddScoped<IProductAttributeAdminApplicationService, InMemoryProductAttributeAdminApplicationService>();
        services.AddScoped<IMediaAssetAdminApplicationService, MediaAssetAdminApplicationService>();

        if (string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            return services.AddPostgreSqlCatalogPersistence(configuration);
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
        services.TryAddScoped<ICurrentActorAccessor, SystemCurrentActorAccessor>();
        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();
        services.AddScoped<IAuditLogRepository, InMemoryAuditLogRepository>();
        services.AddScoped<IAdminUserRepository, InMemoryAdminUserRepository>();
        services.AddScoped<IBrandRepository, InMemoryBrandRepository>();
        services.AddScoped<IIntegrationJobRepository, InMemoryIntegrationJobRepository>();
        services.AddScoped<IOutboxMessageRepository, InMemoryOutboxMessageRepository>();
        services.AddScoped<ICartRepository, InMemoryCartRepository>();
        services.AddScoped<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddScoped<ICompanyRepository, InMemoryCompanyRepository>();
        services.AddScoped<IInventoryBalanceRepository, InMemoryInventoryBalanceRepository>();
        services.AddScoped<IInventoryLocationRepository, InMemoryInventoryLocationRepository>();
        services.AddScoped<IChannelRepository, InMemoryChannelRepository>();
        services.AddScoped<ICategoryRepository, InMemoryCategoryRepository>();
        services.AddScoped<IMarketRepository, InMemoryMarketRepository>();
        services.AddScoped<IMediaAssetRepository, InMemoryMediaAssetRepository>();
        services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
        services.AddScoped<IPriceListRepository, InMemoryPriceListRepository>();
        services.AddScoped<IProductRepository, InMemoryProductRepository>();
        services.AddScoped<IProductAttributeRepository, InMemoryProductAttributeRepository>();
        services.AddScoped<IProductStatusDefinitionRepository, InMemoryProductStatusDefinitionRepository>();
        services.AddScoped<IProductStatusDefinitionApplicationService, ProductStatusDefinitionApplicationService>();
        services.AddScoped<IStorefrontProductProjectionRepository, InMemoryStorefrontProductProjectionRepository>();
        services.AddScoped<IVariantRepository, InMemoryVariantRepository>();
        services.AddScoped<IWebhookSubscriptionRepository, InMemoryWebhookSubscriptionRepository>();
        services.AddScoped<IWebhookDeliveryRepository, InMemoryWebhookDeliveryRepository>();
        return services;
    }

    private static IServiceCollection AddPostgreSqlCatalogPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Platform");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Platform' is required when Persistence:Provider is set to 'PostgreSql'.");
        }

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.TryAddScoped<ICurrentActorAccessor, SystemCurrentActorAccessor>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
        services.AddScoped<IAdminUserRepository, EfAdminUserRepository>();
        services.AddScoped<IBrandRepository, EfBrandRepository>();
        services.AddScoped<IIntegrationJobRepository, EfIntegrationJobRepository>();
        services.AddScoped<IOutboxMessageRepository, EfOutboxMessageRepository>();
        services.AddScoped<ICartRepository, EfCartRepository>();
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<ICompanyRepository, EfCompanyRepository>();
        services.AddScoped<IInventoryBalanceRepository, EfInventoryBalanceRepository>();
        services.AddScoped<IInventoryLocationRepository, EfInventoryLocationRepository>();
        services.AddScoped<IChannelRepository, EfChannelRepository>();
        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<IMarketRepository, EfMarketRepository>();
        services.AddScoped<IMediaAssetRepository, EfMediaAssetRepository>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IPriceListRepository, EfPriceListRepository>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IProductAttributeRepository, EfProductAttributeRepository>();
        services.AddScoped<IProductStatusDefinitionRepository, EfProductStatusDefinitionRepository>();
        services.AddScoped<IProductStatusDefinitionApplicationService, ProductStatusDefinitionApplicationService>();
        services.AddScoped<IStorefrontProductProjectionRepository, EfStorefrontProductProjectionRepository>();
        services.AddScoped<IVariantRepository, EfVariantRepository>();
        services.AddScoped<IWebhookSubscriptionRepository, EfWebhookSubscriptionRepository>();
        services.AddScoped<IWebhookDeliveryRepository, EfWebhookDeliveryRepository>();

        return services;
    }
}
