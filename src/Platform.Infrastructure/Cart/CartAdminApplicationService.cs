using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Cart;
using Platform.Application.Cart.Commands;
using Platform.Application.Cart.Queries;
using Platform.Application.Catalog.Pricing;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Variants;
using Platform.Contracts.Cart;
using Platform.Contracts.Common;
using Platform.Domain.Cart;

namespace Platform.Infrastructure.Cart;

public sealed class CartAdminApplicationService : ICartAdminApplicationService
{
    private readonly ICartRepository _cartRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CartAdminApplicationService(
        ICartRepository cartRepository,
        IPriceListRepository priceListRepository,
        IVariantRepository variantRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _priceListRepository = priceListRepository;
        _variantRepository = variantRepository;
        _productRepository = productRepository;
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

    public async Task<IReadOnlyList<CartPriceUpdate>> ResolveCartPricingAsync(Platform.Domain.Cart.Cart cart, CancellationToken cancellationToken)
    {
        if (cart.Lines.Count == 0)
        {
            throw new RequestValidationException(nameof(cart.Lines), "Cart must contain at least one line.");
        }

        var variants = (await _variantRepository.GetByIdsAsync(cart.Lines.Select(x => x.VariantId).Distinct().ToList(), cancellationToken)).ToDictionary(x => x.Id);
        var products = (await _productRepository.GetByIdsAsync(variants.Values.Select(x => x.ProductId).Distinct().ToList(), cancellationToken)).ToDictionary(x => x.Id);
        var priceLists = await _priceListRepository.ListActiveByMarketAsync(cart.MarketId, cart.CurrencyCode, DateTime.UtcNow, cancellationToken);

        var updates = new List<CartPriceUpdate>(cart.Lines.Count);
        foreach (var line in cart.Lines)
        {
            if (!variants.TryGetValue(line.VariantId, out var variant))
            {
                throw new RequestValidationException(nameof(line.VariantId), $"Unknown variant {line.VariantId}.");
            }

            if (!products.TryGetValue(variant.ProductId, out var product))
            {
                throw new RequestValidationException(nameof(line.VariantId), $"Unknown product for variant {line.VariantId}.");
            }

            var pricing = ResolveVariantPricing(priceLists, line.VariantId, line.Quantity);
            if (pricing is null)
            {
                throw new RequestValidationException(nameof(line.VariantId), $"No active price found for variant {variant.Sku}.");
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

        return updates;
    }

    internal static string ResolveProductName(Platform.Domain.Catalog.Products.Product product)
    {
        return product.Translations.FirstOrDefault()?.Name ?? product.ProductNumber;
    }

    internal static ResolvedPrice? ResolveVariantPricing(IReadOnlyList<Platform.Domain.Catalog.Pricing.PriceList> priceLists, Guid variantId, decimal quantity)
    {
        const decimal defaultVatRate = 0.25m;

        foreach (var priceList in priceLists)
        {
            var entry = priceList.Entries
                .Where(x => string.Equals(x.TargetType, "Variant", StringComparison.OrdinalIgnoreCase) && x.TargetId == variantId)
                .Where(x => x.MinQuantity <= quantity)
                .Where(x => !x.ValidFromUtc.HasValue || x.ValidFromUtc.Value <= DateTime.UtcNow)
                .Where(x => !x.ValidToUtc.HasValue || x.ValidToUtc.Value >= DateTime.UtcNow)
                .OrderByDescending(x => x.MinQuantity)
                .ThenByDescending(x => x.ValidFromUtc)
                .FirstOrDefault();

            if (entry is null)
            {
                continue;
            }

            var unitPrice = priceList.VatIncluded
                ? decimal.Round(entry.Amount / (1m + defaultVatRate), 2, MidpointRounding.AwayFromZero)
                : entry.Amount;

            return new ResolvedPrice(unitPrice, defaultVatRate);
        }

        return null;
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
