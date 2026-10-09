using Microsoft.EntityFrameworkCore;
using Platform.Application.Storefront;
using Platform.Domain.Catalog.Pricing;
using Platform.Domain.Catalog.Products;
using Platform.Domain.Catalog.Variants;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Security;

namespace Platform.Tests;

/// <summary>
/// Seeding shared by the EF scan-query tests and the provider parity tests, so both suites feed
/// the two <see cref="IStorefrontPriceValidityScanRepository"/> implementations the same data.
/// </summary>
internal static class StorefrontPriceValidityScanFixtures
{
    private static readonly Guid ReadyVariantStatusId = Guid.Parse("10000000-0000-0000-0000-000000000102");

    public static PlatformDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PlatformDbContext(options, new SystemCurrentActorAccessor());
    }

    public static Guid AddVariant(PlatformDbContext dbContext, Guid productId, DateTime nowUtc, Guid? variantId = null)
    {
        // Variants share one status definition instance per context: EF tracks it by key and
        // rejects a second instance with the same id.
        var variant = CreateVariant(productId, variantId ?? Guid.NewGuid(), nowUtc, EnsureVariantStatus(dbContext));
        var entry = dbContext.Variants.Add(variant);
        entry.Property<string>("RowVersion").CurrentValue = Guid.NewGuid().ToString("N");
        return variant.Id;
    }

    public static Variant CreateVariant(Guid productId, Guid variantId, DateTime nowUtc)
    {
        return CreateVariant(productId, variantId, nowUtc, CreateVariantStatus());
    }

    private static Variant CreateVariant(Guid productId, Guid variantId, DateTime nowUtc, ProductStatusDefinition status)
    {
        return new Variant(
            variantId,
            productId,
            $"SKU-{variantId:N}",
            null,
            null,
            null,
            status,
            true,
            null,
            null,
            null,
            null,
            nowUtc.AddDays(-10),
            nowUtc.AddDays(-10),
            []);
    }

    private static ProductStatusDefinition EnsureVariantStatus(PlatformDbContext dbContext)
    {
        var tracked = dbContext.ChangeTracker.Entries<ProductStatusDefinition>()
            .Select(x => x.Entity)
            .FirstOrDefault(x => x.Id == ReadyVariantStatusId);
        if (tracked is not null)
        {
            return tracked;
        }

        var status = CreateVariantStatus();
        var entry = dbContext.ProductStatusDefinitions.Add(status);
        entry.Property<string>("RowVersion").CurrentValue = Guid.NewGuid().ToString("N");
        return status;
    }

    private static ProductStatusDefinition CreateVariantStatus()
    {
        return new ProductStatusDefinition(ReadyVariantStatusId, ProductStatusEntityType.Variant, "READY", "Ready", true);
    }

    public static PriceList CreatePriceList(string code, DateTime? validFromUtc, DateTime? validToUtc, DateTime nowUtc)
    {
        return new PriceList(
            Guid.NewGuid(),
            code,
            code,
            "SEK",
            true,
            validFromUtc,
            validToUtc,
            nowUtc.AddDays(-10),
            nowUtc.AddDays(-10));
    }

    public static StorefrontProductProjection CreateProjection(Guid productId, string cultureCode, DateTime projectedAtUtc)
    {
        return new StorefrontProductProjection(
            Guid.NewGuid(),
            productId,
            Guid.NewGuid(),
            "SE",
            cultureCode,
            "SEK",
            $"SKU-{productId:N}",
            $"slug-{productId:N}-{cultureCode}",
            "Hardware",
            "Example",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "[]",
            "[]",
            "[]",
            "[]",
            "[]",
            null,
            "[]",
            "[]",
            true,
            true,
            true,
            "[]",
            "InStock",
            5m,
            false,
            1499m,
            null,
            true,
            "SE_BASE",
            "[]",
            "example",
            "example",
            "sku",
            1499m,
            null,
            projectedAtUtc,
            projectedAtUtc);
    }
}
