using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Cart;
using Platform.Application.Cart.Commands;
using Platform.Application.Cart.Queries;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Pricing;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Tax;
using Platform.Application.Catalog.Variants;
using Platform.Contracts.Cart;
using Platform.Contracts.Common;
using Platform.Domain.Cart;
using Platform.Domain.Catalog.Tax;

namespace Platform.Infrastructure.Cart;

public sealed class CartAdminApplicationService : ICartAdminApplicationService
{
    private readonly ICartRepository _cartRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;
    private readonly IVatRuleRepository _vatRuleRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CartAdminApplicationService(
        ICartRepository cartRepository,
        IPriceListRepository priceListRepository,
        IVariantRepository variantRepository,
        IProductRepository productRepository,
        IVatRuleRepository vatRuleRepository,
        IMarketRepository marketRepository,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _priceListRepository = priceListRepository;
        _variantRepository = variantRepository;
        _productRepository = productRepository;
        _vatRuleRepository = vatRuleRepository;
        _marketRepository = marketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<CartSummaryDto>> ListAsync(ListCartsQuery query, CancellationToken cancellationToken)
    {
        var result = await _cartRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<CartSummaryDto>(
            result.Items.Select(MapSummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<CartDetailsDto?> GetByIdAsync(GetCartByIdQuery query, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(query.CartId, cancellationToken);
        return cart is null ? null : MapDetails(cart);
    }

    public async Task<CartDetailsDto?> RepriceAsync(RepriceCartCommand command, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(command.CartId, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        var updates = await ResolveCartPricingAsync(cart, cancellationToken);
        cart.Reprice(updates, command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(cart);
    }

    public async Task<CartDetailsDto?> ExpireAsync(ExpireCartCommand command, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(command.CartId, cancellationToken);
        if (cart is null)
        {
            return null;
        }

        cart.Expire(command.RowVersion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(cart);
    }

    /// <summary>
    /// The single pricing path for admin repricing, storefront carts, checkout, and manual orders.
    /// Each line gets its net unit price from the market's active price lists and its VAT rate from
    /// the VAT rule for (cart market, product tax category). Missing prices or VAT rules fail the
    /// whole cart with validation errors; no default rate is ever assumed.
    /// </summary>
    public async Task<IReadOnlyList<CartPriceUpdate>> ResolveCartPricingAsync(Platform.Domain.Cart.Cart cart, CancellationToken cancellationToken)
    {
        if (cart.Lines.Count == 0)
        {
            throw new RequestValidationException(nameof(cart.Lines), "Cart must contain at least one line.");
        }

        var variants = (await _variantRepository.GetByIdsAsync(cart.Lines.Select(x => x.VariantId).Distinct().ToList(), cancellationToken)).ToDictionary(x => x.Id);
        var products = (await _productRepository.GetByIdsAsync(variants.Values.Select(x => x.ProductId).Distinct().ToList(), cancellationToken)).ToDictionary(x => x.Id);
        var instantUtc = DateTime.UtcNow;
        var priceLists = await _priceListRepository.ListActiveByMarketAsync(cart.MarketId, cart.CurrencyCode, instantUtc, cancellationToken);
        var vatRates = (await _vatRuleRepository.ListByMarketAsync(cart.MarketId, cancellationToken))
            .ToDictionary(x => x.TaxCategoryCode, x => x.Rate, StringComparer.Ordinal);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var missingVatRules = new List<(string TaxCategoryCode, string Sku)>();
        var updates = new List<CartPriceUpdate>(cart.Lines.Count);
        foreach (var line in cart.Lines)
        {
            if (!variants.TryGetValue(line.VariantId, out var variant))
            {
                AddError(errors, nameof(line.VariantId), $"Unknown variant {line.VariantId}.");
                continue;
            }

            if (!products.TryGetValue(variant.ProductId, out var product))
            {
                AddError(errors, nameof(line.VariantId), $"Unknown product for variant {line.VariantId}.");
                continue;
            }

            var taxCategoryCode = VatRule.NormalizeTaxCategoryCode(product.TaxCategoryCode);
            if (!vatRates.TryGetValue(taxCategoryCode, out var vatRate))
            {
                missingVatRules.Add((taxCategoryCode, variant.Sku));
                continue;
            }

            var pricing = ResolveVariantPricing(priceLists, line.VariantId, line.Quantity, instantUtc, vatRate);
            if (pricing is null)
            {
                AddError(errors, nameof(line.VariantId), $"No active price found for variant {variant.Sku}.");
                continue;
            }

            updates.Add(new CartPriceUpdate(
                line.Id,
                variant.Sku,
                ResolveProductName(product),
                null,
                pricing.UnitPrice,
                pricing.VatRate,
                line.Comment));
        }

        if (missingVatRules.Count > 0)
        {
            var market = await _marketRepository.GetByIdAsync(cart.MarketId, cancellationToken);
            var marketLabel = market?.Code ?? cart.MarketId.ToString();
            foreach (var group in missingVatRules.GroupBy(x => x.TaxCategoryCode, StringComparer.Ordinal))
            {
                var category = group.Key.Length == 0 ? "(none)" : group.Key;
                var skus = string.Join(", ", group.Select(x => x.Sku).Distinct(StringComparer.Ordinal));
                AddError(
                    errors,
                    nameof(VatRule.TaxCategoryCode),
                    $"No VAT rule is configured for tax category '{category}' in market '{marketLabel}'; cannot price variant(s) {skus}.");
            }
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.Ordinal));
        }

        return updates;
    }

    internal static string ResolveProductName(Platform.Domain.Catalog.Products.Product product)
    {
        return product.Translations.FirstOrDefault()?.Name ?? product.ProductNumber;
    }

    /// <summary>
    /// Selects the first price list (already ordered by market priority) with an eligible entry: the
    /// highest quantity break not above <paramref name="quantity"/> that is valid at
    /// <paramref name="instantUtc"/>. VAT-inclusive amounts are converted to net using
    /// <paramref name="vatRate"/> and rounded to two decimals, midpoint away from zero; net amounts are
    /// used as-is.
    /// </summary>
    internal static ResolvedPrice? ResolveVariantPricing(
        IReadOnlyList<Platform.Domain.Catalog.Pricing.PriceList> priceLists,
        Guid variantId,
        decimal quantity,
        DateTime instantUtc,
        decimal vatRate)
    {
        foreach (var priceList in priceLists)
        {
            var entry = priceList.Entries
                .Where(x => string.Equals(x.TargetType, "Variant", StringComparison.OrdinalIgnoreCase) && x.TargetId == variantId)
                .Where(x => x.MinQuantity <= quantity)
                .Where(x => x.IsValidAt(instantUtc))
                .OrderByDescending(x => x.MinQuantity)
                .ThenByDescending(x => x.ValidFromUtc)
                .FirstOrDefault();

            if (entry is null)
            {
                continue;
            }

            var unitPrice = priceList.VatIncluded
                ? decimal.Round(entry.Amount / (1m + vatRate), 2, MidpointRounding.AwayFromZero)
                : entry.Amount;

            return new ResolvedPrice(unitPrice, vatRate);
        }

        return null;
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        if (!messages.Contains(message, StringComparer.Ordinal))
        {
            messages.Add(message);
        }
    }

    private static CartSummaryDto MapSummary(Platform.Domain.Cart.Cart cart)
    {
        return new CartSummaryDto(
            cart.Id,
            cart.CustomerId,
            cart.CompanyId,
            cart.MarketId,
            cart.CurrencyCode,
            cart.CultureCode,
            cart.Email,
            cart.Status,
            cart.GrandTotal,
            cart.Lines.Count,
            cart.CreatedAtUtc,
            cart.UpdatedAtUtc,
            cart.RowVersion);
    }

    private static CartDetailsDto MapDetails(Platform.Domain.Cart.Cart cart)
    {
        return new CartDetailsDto(
            cart.Id,
            cart.CustomerId,
            cart.CompanyId,
            cart.MarketId,
            cart.CurrencyCode,
            cart.CultureCode,
            cart.Email,
            cart.Status,
            cart.Subtotal,
            cart.VatTotal,
            cart.GrandTotal,
            cart.ExpiresAtUtc,
            cart.Lines.Select(x => new CartLineDto(x.Id, x.VariantId, x.Sku, x.ProductName, x.VariantDescription, x.Quantity, x.UnitPrice, x.VatRate, x.LineTotal, x.Comment)).ToList(),
            cart.Addresses.Select(x => new CartAddressDto(x.Id, x.Type, x.FirstName, x.LastName, x.CompanyName, x.Line1, x.Line2, x.PostalCode, x.City, x.Region, x.CountryCode, x.Email, x.Phone)).ToList(),
            cart.CreatedAtUtc,
            cart.UpdatedAtUtc,
            cart.RowVersion);
    }

    public sealed record ResolvedPrice(decimal UnitPrice, decimal VatRate);
}
