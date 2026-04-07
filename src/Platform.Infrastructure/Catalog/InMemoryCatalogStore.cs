using System.Collections.Concurrent;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Variants;

namespace Platform.Infrastructure.Catalog;

public sealed class InMemoryCatalogStore
{
    public InMemoryCatalogStore()
    {
        Seed();
    }

    public ConcurrentDictionary<Guid, Product> Products { get; } = new();
    public ConcurrentDictionary<Guid, Variant> Variants { get; } = new();

    public IReadOnlyDictionary<Guid, ProductStatusDefinition> Statuses { get; } =
        new Dictionary<Guid, ProductStatusDefinition>
        {
            [Guid.Parse("10000000-0000-0000-0000-000000000001")] = new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "DRAFT", "Draft", false),
            [Guid.Parse("10000000-0000-0000-0000-000000000002")] = new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "READY", "Ready", true),
            [Guid.Parse("10000000-0000-0000-0000-000000000003")] = new(Guid.Parse("10000000-0000-0000-0000-000000000003"), "COMING_SOON", "Coming Soon", false),
            [Guid.Parse("10000000-0000-0000-0000-000000000102")] = new(Guid.Parse("10000000-0000-0000-0000-000000000102"), "READY", "Ready", true)
        };

    private void Seed()
    {
        var readyProductStatus = Statuses[Guid.Parse("10000000-0000-0000-0000-000000000002")];
        var readyVariantStatus = Statuses[Guid.Parse("10000000-0000-0000-0000-000000000102")];
        var now = DateTime.UtcNow;

        var product = new Product(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            "Hardware",
            "SKU-EXAMPLE-1",
            "example-drill",
            null,
            readyProductStatus,
            "STANDARD",
            "pcs",
            true,
            1.8m,
            28.0m,
            8.0m,
            22.0m,
            now.AddMinutes(-30),
            now.AddMinutes(-10));

        product.UpsertTranslation(
            "en-GB",
            "Example Drill",
            "Compact and powerful drill for demanding work.",
            "A compact drill designed for demanding work and reliable day-to-day use.",
            "Example Drill | Demo",
            "Compact and powerful drill for demanding work.");

        Products[product.Id] = product;

        var variant = new Variant(
            Guid.Parse("50000000-0000-0000-0000-000000000011"),
            product.Id,
            "SKU-EXAMPLE-1-BLACK",
            "1234567890123",
            "ACME-DRILL-BLK",
            "1234567890123",
            readyVariantStatus,
            true,
            1.8m,
            28.0m,
            8.0m,
            22.0m,
            now.AddMinutes(-25),
            now.AddMinutes(-10),
            []);

        Variants[variant.Id] = variant;
    }
}
