using Platform.Application.Abstractions.Errors;
using Platform.Application.Cart.Commands;
using Platform.Application.Catalog.Tax.Commands;
using Platform.Application.Orders.Commands;
using Platform.Application.Orders.Queries;
using Platform.Application.Storefront;
using Platform.Contracts.Storefront;

namespace Platform.Tests;

public sealed class VatCartPricingTests
{
    [Fact]
    public async Task StorefrontCart_ResolvesRatePerLineForDifferentTaxCategories()
    {
        var rig = new VatPricingTestRig();
        rig.AddVatRule(VatPricingTestRig.SeMarketId, "REDUCED_FOOD", 0.12m);
        var foodVariantId = rig.AddProduct(1, "reduced_food", 112.00m);
        rig.Project();

        var result = await rig.CreateStorefrontCartAsync((VatPricingTestRig.SeededVariantId, 1m), (foodVariantId, 1m));

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        var cart = result.Cart!;
        var standardLine = cart.Lines.Single(x => x.VariantId == VatPricingTestRig.SeededVariantId);
        var foodLine = cart.Lines.Single(x => x.VariantId == foodVariantId);
        Assert.Equal(0.25m, standardLine.VatRate);
        Assert.Equal(1199.20m, standardLine.UnitPrice);
        Assert.Equal(0.12m, foodLine.VatRate);
        Assert.Equal(100.00m, foodLine.UnitPrice);
        Assert.Equal(1299.20m, cart.Subtotal);
        Assert.Equal(311.80m, cart.VatTotal);
        Assert.Equal(1611.00m, cart.GrandTotal);
    }

    [Fact]
    public async Task ManualOrders_ResolveSameCategoryDifferentlyPerMarket()
    {
        var rig = new VatPricingTestRig();
        var fiMarket = rig.AddMarket(Guid.Parse("62000000-0000-0000-0000-0000000000f1"), "FI", "EUR", "fi-FI");
        rig.AddVatRule(fiMarket.Id, "STANDARD", 0.255m);
        var fiGross = rig.AddPriceList("FI_BASE_GROSS", "EUR", vatIncluded: true, fiMarket.Id, priority: 0);
        fiGross.UpsertEntry(null, "Variant", VatPricingTestRig.SeededVariantId, 1, 125.50m, null, null, null, fiGross.RowVersion);
        var fiWarehouse = rig.AddLocation(fiMarket.Id, "FI-WH");
        rig.AddBalance(fiWarehouse.Id, VatPricingTestRig.SeededVariantId, 10m);

        var seOrder = await rig.Orders.CreateAsync(ManualOrder(VatPricingTestRig.SeMarketId, "SEK", 1m), "tester", CancellationToken.None);
        var fiOrder = await rig.Orders.CreateAsync(ManualOrder(fiMarket.Id, "EUR", 1m), "tester", CancellationToken.None);

        Assert.Equal(0.25m, seOrder.Lines.Single().VatRate);
        Assert.Equal(1199.20m, seOrder.Subtotal);
        Assert.Equal(299.80m, seOrder.VatTotal);
        Assert.Equal(1499.00m, seOrder.GrandTotal);

        Assert.Equal(0.255m, fiOrder.Lines.Single().VatRate);
        Assert.Equal(100.00m, fiOrder.Lines.Single().UnitPrice);
        Assert.Equal(25.50m, fiOrder.VatTotal);
        Assert.Equal(125.50m, fiOrder.GrandTotal);
    }

    [Fact]
    public async Task ExplicitZeroRate_IsAppliedWithoutReducingTheListAmount()
    {
        var rig = new VatPricingTestRig();
        rig.AddVatRule(VatPricingTestRig.SeMarketId, "ZERO", 0m);
        var zeroVariantId = rig.AddProduct(2, "ZERO", 50.00m);
        rig.Project();

        var result = await rig.CreateStorefrontCartAsync((zeroVariantId, 3m));

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        var line = Assert.Single(result.Cart!.Lines);
        Assert.Equal(0m, line.VatRate);
        Assert.Equal(50.00m, line.UnitPrice);
        Assert.Equal(150.00m, result.Cart.Subtotal);
        Assert.Equal(0m, result.Cart.VatTotal);
        Assert.Equal(150.00m, result.Cart.GrandTotal);
    }

    [Fact]
    public async Task MissingRule_FailsStorefrontCartCreationWithClearError()
    {
        var rig = new VatPricingTestRig();
        var booksVariantId = rig.AddProduct(3, "BOOKS", 200.00m);
        rig.Project();

        var result = await rig.CreateStorefrontCartAsync((VatPricingTestRig.SeededVariantId, 1m), (booksVariantId, 1m));

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, result.Status);
        var message = Assert.Single(result.Errors["TaxCategoryCode"]);
        Assert.Contains("'BOOKS'", message, StringComparison.Ordinal);
        Assert.Contains("'SE'", message, StringComparison.Ordinal);
        Assert.Contains("SKU-VAT-3-STD", message, StringComparison.Ordinal);
        Assert.DoesNotContain(rig.Store.Carts.Values, x => x.Lines.Any(l => l.VariantId == booksVariantId));
    }

    [Fact]
    public async Task MissingRule_NeverFallsBackToTwentyFivePercentOnRepriceOrManualOrder()
    {
        var rig = new VatPricingTestRig();
        var seededCart = rig.Store.Carts[Guid.Parse("78000000-0000-0000-0000-000000000001")];
        var rowVersionBefore = seededCart.RowVersion;
        rig.Store.VatRules.Clear();

        var repriceError = await Assert.ThrowsAsync<RequestValidationException>(() =>
            rig.CartPricing.RepriceAsync(new RepriceCartCommand(seededCart.Id, seededCart.RowVersion), CancellationToken.None));
        var orderError = await Assert.ThrowsAsync<RequestValidationException>(() =>
            rig.Orders.CreateAsync(ManualOrder(VatPricingTestRig.SeMarketId, "SEK", 1m), "tester", CancellationToken.None));

        Assert.Contains("'STANDARD'", Assert.Single(repriceError.Errors["TaxCategoryCode"]), StringComparison.Ordinal);
        Assert.Contains("'STANDARD'", Assert.Single(orderError.Errors["TaxCategoryCode"]), StringComparison.Ordinal);
        Assert.Equal(rowVersionBefore, seededCart.RowVersion);
        Assert.Single(rig.Store.Orders);
    }

    [Fact]
    public async Task VatExclusivePriceList_UsesAmountAsNetAndKeepsListPriority()
    {
        var rig = new VatPricingTestRig();
        var baseList = rig.SeBasePriceList;
        baseList.UpsertMarketAssignment(VatPricingTestRig.SeMarketId, 10, true, baseList.RowVersion);
        var netCampaign = rig.AddPriceList("SE_CAMPAIGN_NET", "SEK", vatIncluded: false, VatPricingTestRig.SeMarketId, priority: 5);
        netCampaign.UpsertEntry(null, "Variant", VatPricingTestRig.SeededVariantId, 1, 1000.00m, null, null, null, netCampaign.RowVersion);

        var campaignOrder = await rig.Orders.CreateAsync(ManualOrder(VatPricingTestRig.SeMarketId, "SEK", 1m), "tester", CancellationToken.None);

        Assert.Equal(1000.00m, campaignOrder.Lines.Single().UnitPrice);
        Assert.Equal(0.25m, campaignOrder.Lines.Single().VatRate);
        Assert.Equal(250.00m, campaignOrder.VatTotal);
        Assert.Equal(1250.00m, campaignOrder.GrandTotal);

        // An expired entry on the higher-priority net list falls through to the gross base list.
        var campaignEntry = netCampaign.Entries.Single();
        netCampaign.UpsertEntry(campaignEntry.Id, "Variant", VatPricingTestRig.SeededVariantId, 1, 1000.00m, null, null, DateTime.UtcNow.AddDays(-1), netCampaign.RowVersion);

        var baseOrder = await rig.Orders.CreateAsync(ManualOrder(VatPricingTestRig.SeMarketId, "SEK", 1m), "tester", CancellationToken.None);

        Assert.Equal(1199.20m, baseOrder.Lines.Single().UnitPrice);
        Assert.Equal(1499.00m, baseOrder.GrandTotal);
    }

    [Theory]
    [InlineData("1.5", "8.92", "13.38", "1.61", "14.99")]
    [InlineData("2.5", "8.00", "20.00", "2.40", "22.40")]
    public async Task FractionalQuantities_RespectQuantityBreaksAndRounding(
        string quantity,
        string expectedUnitPrice,
        string expectedSubtotal,
        string expectedVat,
        string expectedGrandTotal)
    {
        var rig = new VatPricingTestRig();
        rig.AddVatRule(VatPricingTestRig.SeMarketId, "FOOD", 0.12m);
        var variantId = rig.AddProduct(4, "FOOD", 9.99m);
        var baseList = rig.SeBasePriceList;
        baseList.UpsertEntry(null, "Variant", variantId, 2, 8.96m, null, null, null, baseList.RowVersion);
        rig.Project();

        var result = await rig.CreateStorefrontCartAsync((variantId, decimal.Parse(quantity, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(StorefrontContextResolutionStatus.Success, result.Status);
        var line = Assert.Single(result.Cart!.Lines);
        Assert.Equal(Dec(expectedUnitPrice), line.UnitPrice);
        Assert.Equal(0.12m, line.VatRate);
        Assert.Equal(Dec(expectedSubtotal), result.Cart.Subtotal);
        Assert.Equal(Dec(expectedVat), result.Cart.VatTotal);
        Assert.Equal(Dec(expectedGrandTotal), result.Cart.GrandTotal);
    }

    [Fact]
    public async Task OrderSnapshot_IsUnchangedAfterRuleUpdateAndDeletion()
    {
        var rig = new VatPricingTestRig();
        rig.Project();
        var created = await rig.CreateStorefrontCartAsync((VatPricingTestRig.SeededVariantId, 2m));
        var checkout = await rig.Storefront.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart!.Id, created.Cart.RowVersion, created.Cart.CartAccessToken),
            CancellationToken.None);
        Assert.Equal(StorefrontContextResolutionStatus.Success, checkout.Status);
        var order = checkout.Order!;
        Assert.Equal(0.25m, order.Lines.Single().VatRate);
        Assert.Equal(599.60m, order.VatTotal);

        var rule = rig.Store.VatRules[VatPricingTestRig.SeStandardVatRuleId];
        var updated = await rig.VatRules.UpdateAsync(new UpdateVatRuleCommand(rule.Id, 0.12m, null, rule.RowVersion), CancellationToken.None);

        var reloaded = await rig.Orders.GetByIdAsync(new GetOrderByIdQuery(order.Id), CancellationToken.None);
        Assert.Equal(0.25m, reloaded!.Lines.Single().VatRate);
        Assert.Equal(order.Subtotal, reloaded.Subtotal);
        Assert.Equal(599.60m, reloaded.VatTotal);
        Assert.Equal(order.GrandTotal, reloaded.GrandTotal);

        // New carts pick up the changed rule; the placed order does not.
        var afterChange = await rig.CreateStorefrontCartAsync((VatPricingTestRig.SeededVariantId, 1m));
        Assert.Equal(0.12m, afterChange.Cart!.Lines.Single().VatRate);
        Assert.Equal(1338.39m, afterChange.Cart.Lines.Single().UnitPrice);

        // Idempotent checkout returns the original snapshot even once the rule is gone.
        await rig.VatRules.DeleteAsync(new DeleteVatRuleCommand(rule.Id, updated!.RowVersion), CancellationToken.None);
        var repeat = await rig.Storefront.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart.Id, created.Cart.RowVersion, created.Cart.CartAccessToken),
            CancellationToken.None);
        Assert.Equal(StorefrontContextResolutionStatus.Success, repeat.Status);
        Assert.Equal(order.Id, repeat.Order!.Id);
        Assert.Equal(0.25m, repeat.Order.Lines.Single().VatRate);
        Assert.Equal(599.60m, repeat.Order.VatTotal);
    }

    [Fact]
    public async Task Checkout_RepricesWithCurrentRuleAndFailsWithoutConvertingWhenRuleIsMissing()
    {
        var rig = new VatPricingTestRig();
        rig.Project();
        var created = await rig.CreateStorefrontCartAsync((VatPricingTestRig.SeededVariantId, 1m));
        rig.Store.VatRules.Clear();

        var checkout = await rig.Storefront.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart!.Id, created.Cart.RowVersion, created.Cart.CartAccessToken),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, checkout.Status);
        Assert.Contains("TaxCategoryCode", checkout.Errors.Keys);
        Assert.Equal("Active", rig.Store.Carts[created.Cart.Id].Status);
        Assert.Equal(created.Cart.RowVersion, rig.Store.Carts[created.Cart.Id].RowVersion);
        Assert.DoesNotContain(rig.Store.Orders.Values, x => x.SourceCartId == created.Cart.Id);

        // Once configured, the same cart and row version check out at the configured rate.
        rig.AddVatRule(VatPricingTestRig.SeMarketId, "STANDARD", 0.06m);
        var retried = await rig.Storefront.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart.Id, created.Cart.RowVersion, created.Cart.CartAccessToken),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, retried.Status);
        Assert.Equal(0.06m, retried.Order!.Lines.Single().VatRate);
        Assert.Equal(1414.15m, retried.Order.Subtotal);
        Assert.Equal(84.85m, retried.Order.VatTotal);
        Assert.Equal(1499.00m, retried.Order.GrandTotal);
    }

    [Fact]
    public async Task Checkout_RequiresOwnershipBeforeVatResolutionAndRejectsStaleRowVersion()
    {
        var rig = new VatPricingTestRig();
        rig.Project();
        var created = await rig.CreateStorefrontCartAsync((VatPricingTestRig.SeededVariantId, 1m));
        rig.Store.VatRules.Clear();

        var withoutToken = await rig.Storefront.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart!.Id, created.Cart.RowVersion, null),
            CancellationToken.None);
        var invalidToken = await rig.Storefront.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart.Id, created.Cart.RowVersion, "not-a-valid-token"),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Unauthorized, withoutToken.Status);
        Assert.Equal(StorefrontContextResolutionStatus.Unauthorized, invalidToken.Status);
        Assert.Empty(withoutToken.Errors);

        rig.AddVatRule(VatPricingTestRig.SeMarketId, "STANDARD", 0.25m);
        var staleRowVersion = await rig.Storefront.CheckoutAsync(
            new CheckoutStorefrontCartCommand(created.Cart.Id, "stale-row-version", created.Cart.CartAccessToken),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.ValidationFailed, staleRowVersion.Status);
        Assert.Contains("RowVersion", staleRowVersion.Errors.Keys);
        Assert.DoesNotContain(rig.Store.Orders.Values, x => x.SourceCartId == created.Cart.Id);
    }

    [Fact]
    public async Task StorefrontReprice_PicksUpChangedRateForExistingCart()
    {
        var rig = new VatPricingTestRig();
        rig.Project();
        var created = await rig.CreateStorefrontCartAsync((VatPricingTestRig.SeededVariantId, 1m));
        var rule = rig.Store.VatRules[VatPricingTestRig.SeStandardVatRuleId];
        await rig.VatRules.UpdateAsync(new UpdateVatRuleCommand(rule.Id, 0.12m, null, rule.RowVersion), CancellationToken.None);

        var repriced = await rig.Storefront.RepriceAsync(
            new RepriceStorefrontCartCommand(created.Cart!.Id, created.Cart.RowVersion, created.Cart.CartAccessToken),
            CancellationToken.None);

        Assert.Equal(StorefrontContextResolutionStatus.Success, repriced.Status);
        Assert.Equal(0.12m, repriced.Cart!.Lines.Single().VatRate);
        Assert.Equal(1338.39m, repriced.Cart.Subtotal);
        Assert.Equal(160.61m, repriced.Cart.VatTotal);
        Assert.Equal(1499.00m, repriced.Cart.GrandTotal);
    }

    [Fact]
    public async Task StorefrontPriceMetadata_StaysTheListAmountWithVatIncludedFlag()
    {
        var rig = new VatPricingTestRig();
        rig.AddVatRule(VatPricingTestRig.SeMarketId, "FOOD", 0.12m);
        var variantId = rig.AddProduct(5, "FOOD", 112.00m);
        rig.Project();

        var projection = rig.Store.StorefrontProductProjections.Values.First(x => x.ProductId == Guid.Parse("51000000-0000-0000-0000-000000000005"));
        var cart = await rig.CreateStorefrontCartAsync((variantId, 1m));

        // The projection advertises the configured list amount; the cart's gross total reproduces it.
        Assert.Equal(112.00m, projection.PriceAmount);
        Assert.True(projection.VatIncluded);
        Assert.Equal(112.00m, cart.Cart!.GrandTotal);
    }

    private static CreateOrderCommand ManualOrder(Guid marketId, string currencyCode, decimal quantity)
    {
        return new CreateOrderCommand(
            CartId: null,
            CartRowVersion: null,
            CustomerId: null,
            CompanyId: null,
            MarketId: marketId,
            CurrencyCode: currencyCode,
            CultureCode: "sv-SE",
            Email: "buyer@example.com",
            Lines: [new CreateOrderLineItem(VatPricingTestRig.SeededVariantId, quantity, null)],
            Addresses: []);
    }

    private static decimal Dec(string value)
    {
        return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
