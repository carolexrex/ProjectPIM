namespace Platform.Domain.Integrations;

public static class WebhookEventTypes
{
    public const string BrandCreated = "catalog.brand.created";
    public const string BrandUpdated = "catalog.brand.updated";
    public const string ProductCreated = "catalog.product.created";
    public const string ProductUpdated = "catalog.product.updated";
    public const string PriceListCreated = "pricing.price-list.created";
    public const string PriceListUpdated = "pricing.price-list.updated";
    public const string IntegrationJobCompleted = "integration.job.completed";
    public const string IntegrationJobFailed = "integration.job.failed";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        BrandCreated,
        BrandUpdated,
        ProductCreated,
        ProductUpdated,
        PriceListCreated,
        PriceListUpdated,
        IntegrationJobCompleted,
        IntegrationJobFailed
    };
}
