using Platform.Application.Storefront;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Categories;
using Platform.Infrastructure.Catalog.Channels;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

public sealed class StorefrontCategoryApplicationServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsLocalizedCategoryTree()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.ListAsync(
            new GetStorefrontCategoriesQuery("WEB-SE", "SE", "en-GB", null, null),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        Assert.NotNull(result.Categories);
        var root = Assert.Single(result.Categories!);
        Assert.Equal("TOOLS", root.Code);
        Assert.Equal("tools", root.Slug);
        Assert.Equal("Tools", root.Name);
        var child = Assert.Single(root.Children);
        Assert.Equal("DRILLS", child.Code);
        Assert.Equal("drills", child.Slug);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsDetailsWithBreadcrumbs()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.GetBySlugAsync(
            new GetStorefrontCategoryBySlugQuery("drills", "WEB-SE", "SE", "en-GB", null, null),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        Assert.NotNull(result.Category);
        Assert.Equal("DRILLS", result.Category!.Code);
        Assert.Equal("Drills", result.Category.Name);
        Assert.Equal(2, result.Category.Breadcrumbs.Count);
        Assert.Equal("TOOLS", result.Category.Breadcrumbs[0].Code);
        Assert.Equal("DRILLS", result.Category.Breadcrumbs[1].Code);
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNotFoundForUnknownSlug()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.GetBySlugAsync(
            new GetStorefrontCategoryBySlugQuery("missing-category", "WEB-SE", "SE", "en-GB", null, null),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.NotFound, result.Status);
        Assert.Equal("Category", result.ResourceName);
    }

    private static StorefrontCategoryApplicationService CreateService(InMemoryCatalogStore store)
    {
        var contextService = new StorefrontContextApplicationService(
            new InMemoryChannelRepository(store),
            new InMemoryMarketRepository(store));

        return new StorefrontCategoryApplicationService(
            new InMemoryCategoryRepository(store),
            contextService);
    }
}
