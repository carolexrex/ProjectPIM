using Microsoft.EntityFrameworkCore;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Catalog.Tax.Commands;
using Platform.Application.Catalog.Tax.Queries;
using Platform.Domain.Catalog.Markets;
using Platform.Domain.Common;
using Platform.Infrastructure.Catalog;
using Platform.Infrastructure.Catalog.Markets;
using Platform.Infrastructure.Catalog.Tax;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Security;

namespace Platform.Tests;

public sealed class VatRuleAdminApplicationServiceTests
{
    private static readonly Guid SeMarketId = VatPricingTestRig.SeMarketId;

    [Fact]
    public async Task Create_NormalizesTaxCategoryAndAllowsExplicitZero()
    {
        var service = CreateInMemoryService(new InMemoryCatalogStore());

        var created = await service.CreateAsync(new CreateVatRuleCommand(SeMarketId, "  exempt ", 0m, " Exempt goods "), CancellationToken.None);

        Assert.Equal("EXEMPT", created.TaxCategoryCode);
        Assert.Equal(0m, created.Rate);
        Assert.Equal("SE", created.MarketCode);
        Assert.Equal("Exempt goods", created.Description);
        Assert.False(string.IsNullOrWhiteSpace(created.RowVersion));
    }

    [Fact]
    public async Task Create_RejectsDuplicateMarketAndTaxCategoryCaseInsensitively()
    {
        var service = CreateInMemoryService(new InMemoryCatalogStore());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateVatRuleCommand(SeMarketId, "standard", 0.12m, null), CancellationToken.None));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1")]
    [InlineData("1.25")]
    [InlineData("0.12345")]
    public async Task Create_RejectsInvalidRates(string rate)
    {
        var service = CreateInMemoryService(new InMemoryCatalogStore());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.CreateAsync(
                new CreateVatRuleCommand(SeMarketId, "FOOD", decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture), null),
                CancellationToken.None));

        Assert.Contains("Rate", exception.Errors.Keys);
    }

    [Fact]
    public async Task Create_RejectsUnknownMarketAndBlankTaxCategory()
    {
        var service = CreateInMemoryService(new InMemoryCatalogStore());

        var unknownMarket = await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.CreateAsync(new CreateVatRuleCommand(Guid.NewGuid(), "FOOD", 0.12m, null), CancellationToken.None));
        var blankCategory = await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.CreateAsync(new CreateVatRuleCommand(SeMarketId, "   ", 0.12m, null), CancellationToken.None));

        Assert.Contains("MarketId", unknownMarket.Errors.Keys);
        Assert.Contains("TaxCategoryCode", blankCategory.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAndDelete_RequireCurrentRowVersion()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateInMemoryService(store);
        var rule = store.VatRules[VatPricingTestRig.SeStandardVatRuleId];
        var originalRowVersion = rule.RowVersion;

        var updated = await service.UpdateAsync(new UpdateVatRuleCommand(rule.Id, 0.12m, "Reduced", originalRowVersion), CancellationToken.None);

        Assert.Equal(0.12m, updated!.Rate);
        Assert.NotEqual(originalRowVersion, updated.RowVersion);
        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            service.UpdateAsync(new UpdateVatRuleCommand(rule.Id, 0.06m, null, originalRowVersion), CancellationToken.None));
        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            service.DeleteAsync(new DeleteVatRuleCommand(rule.Id, originalRowVersion), CancellationToken.None));
        Assert.Equal(0.12m, store.VatRules[rule.Id].Rate);

        Assert.True(await service.DeleteAsync(new DeleteVatRuleCommand(rule.Id, updated.RowVersion), CancellationToken.None));
        Assert.False(store.VatRules.ContainsKey(rule.Id));
        Assert.False(await service.DeleteAsync(new DeleteVatRuleCommand(rule.Id, updated.RowVersion), CancellationToken.None));
        Assert.Null(await service.UpdateAsync(new UpdateVatRuleCommand(rule.Id, 0.06m, null, updated.RowVersion), CancellationToken.None));
    }

    [Fact]
    public async Task List_FiltersByMarketAndNormalizedTaxCategory()
    {
        var store = new InMemoryCatalogStore();
        var service = CreateInMemoryService(store);
        var now = DateTime.UtcNow;
        var fi = new Market(Guid.NewGuid(), "FI", "Finland", "EUR", "fi-FI", "Gross", now, now);
        store.Markets[fi.Id] = fi;
        await service.CreateAsync(new CreateVatRuleCommand(fi.Id, "STANDARD", 0.255m, null), CancellationToken.None);
        await service.CreateAsync(new CreateVatRuleCommand(SeMarketId, "FOOD", 0.12m, null), CancellationToken.None);

        var standard = await service.ListAsync(new ListVatRulesQuery(null, "standard"), CancellationToken.None);
        var seOnly = await service.ListAsync(new ListVatRulesQuery(SeMarketId, null), CancellationToken.None);

        Assert.Equal(2, standard.Total);
        Assert.Contains(standard.Items, x => x.MarketCode == "FI" && x.Rate == 0.255m);
        Assert.Equal(["FOOD", "STANDARD"], seOnly.Items.Select(x => x.TaxCategoryCode).ToArray());
    }

    [Fact]
    public async Task EfProvider_MapsStaleConcurrentUpdateToConcurrencyException()
    {
        var databaseName = $"vat-rules-{Guid.NewGuid():N}";
        Guid ruleId;
        await using (var setup = CreateDbContext(databaseName))
        {
            var market = new Market(SeMarketId, "SE", "Sweden", "SEK", "sv-SE", "Gross", DateTime.UtcNow, DateTime.UtcNow);
            setup.Markets.Add(market);
            await setup.SaveChangesAsync();
            var created = await CreateEfService(setup).CreateAsync(new CreateVatRuleCommand(SeMarketId, " standard ", 0.25m, null), CancellationToken.None);
            ruleId = created.Id;
        }

        await using var first = CreateDbContext(databaseName);
        await using var second = CreateDbContext(databaseName);
        var firstService = CreateEfService(first);
        var secondService = CreateEfService(second);
        var firstLoaded = await firstService.GetByIdAsync(new GetVatRuleByIdQuery(ruleId), CancellationToken.None);
        var secondLoaded = await secondService.GetByIdAsync(new GetVatRuleByIdQuery(ruleId), CancellationToken.None);

        await firstService.UpdateAsync(new UpdateVatRuleCommand(ruleId, 0.12m, null, firstLoaded!.RowVersion), CancellationToken.None);

        // The second writer loaded the same row version, so the domain check passes and the
        // database concurrency token is what rejects the lost update.
        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            secondService.UpdateAsync(new UpdateVatRuleCommand(ruleId, 0.06m, null, secondLoaded!.RowVersion), CancellationToken.None));

        await using var verify = CreateDbContext(databaseName);
        var stored = await verify.VatRules.AsNoTracking().SingleAsync();
        Assert.Equal(0.12m, stored.Rate);
        Assert.Equal("STANDARD", stored.TaxCategoryCode);
        Assert.Single(await new EfVatRuleRepository(verify).ListByMarketAsync(SeMarketId, CancellationToken.None));
        Assert.NotNull(await new EfVatRuleRepository(verify).GetByMarketAndTaxCategoryAsync(SeMarketId, "Standard", CancellationToken.None));
    }

    private static VatRuleAdminApplicationService CreateInMemoryService(InMemoryCatalogStore store)
    {
        return new VatRuleAdminApplicationService(
            new InMemoryVatRuleRepository(store),
            new InMemoryMarketRepository(store),
            new InMemoryUnitOfWork());
    }

    private static VatRuleAdminApplicationService CreateEfService(PlatformDbContext dbContext)
    {
        return new VatRuleAdminApplicationService(
            new EfVatRuleRepository(dbContext),
            new EfMarketRepository(dbContext),
            new EfUnitOfWork(dbContext));
    }

    private static PlatformDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new PlatformDbContext(options, new SystemCurrentActorAccessor());
    }
}
