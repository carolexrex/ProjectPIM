using Platform.Application.Storefront;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Channels;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Storefront;

namespace Platform.Tests;

public sealed class StorefrontContextApplicationServiceTests
{
    [Fact]
    public async Task GetContextAsync_ResolvesChannelMarketAndFallsBackToDefaultCultureAndCurrency()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.GetContextAsync(
            new GetStorefrontContextQuery("WEB-SE", null, "fr-FR", "EUR", null),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        Assert.NotNull(result.Context);
        Assert.Equal("WEB-SE", result.Context!.Channel!.Code);
        Assert.Equal("SE", result.Context.Market.Code);
        Assert.Equal("sv-SE", result.Context.ActiveCultureCode);
        Assert.Equal("SEK", result.Context.ActiveCurrencyCode);
        Assert.Contains("sv-SE", result.Context.AvailableCultureCodes);
        Assert.Contains("SEK", result.Context.AvailableCurrencyCodes);
    }

    [Fact]
    public async Task GetContextAsync_UsesHostNameToResolveChannel()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateService(store);

        var result = await service.GetContextAsync(
            new GetStorefrontContextQuery(null, null, "en-GB", null, "se.example.com"),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        Assert.NotNull(result.Context);
        Assert.Equal("WEB-SE", result.Context!.Channel!.Code);
        Assert.Equal("SE", result.Context.Market.Code);
        Assert.Equal("sv-SE", result.Context.ActiveCultureCode);
    }

    [Fact]
    public async Task GetContextAsync_ReturnsValidationFailureWhenChannelMapsToMultipleMarketsWithoutExplicitMarket()
    {
        var store = new InMemoryCatalogStore();
        var now = DateTime.UtcNow;
        var channel = store.Channels[Guid.Parse("63000000-0000-0000-0000-000000000001")];
        var secondMarket = new Platform.Domain.Catalog.Markets.Market(
            Guid.Parse("62000000-0000-0000-0000-000000000002"),
            "NO",
            "Norway",
            "NOK",
            "nb-NO",
            "Gross",
            now,
            now);
        store.Markets[secondMarket.Id] = secondMarket;
        channel.UpsertMarketAssignment(secondMarket.Id, channel.RowVersion);

        var service = CreateService(store);

        var result = await service.GetContextAsync(
            new GetStorefrontContextQuery("WEB-SE", null, null, null, null),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, result.Status);
        Assert.Contains(nameof(GetStorefrontContextQuery.MarketCode), result.Errors.Keys);
    }

    private static StorefrontContextApplicationService CreateService(InMemoryCatalogStore store)
    {
        return new StorefrontContextApplicationService(
            new InMemoryChannelRepository(store),
            new InMemoryMarketRepository(store));
    }
}
